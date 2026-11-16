# 14 - Authorization code flow with OpenIddict (OpenID Connect login)

Users log in to an MVC app through a separate **auth server** - the app never sees a password. The flow is the OAuth 2.0 **authorization code flow with PKCE**, plus **OpenID Connect** to tell the app who logged in. The auth server is built with [OpenIddict](https://documentation.openiddict.com/); the only "resource" is its **userinfo endpoint**, which returns the user's name and permissions for the access token.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Two terminals:

```bash
cd 14-OpenIddictAuthCode
dotnet run --project AuthServer      # https://localhost:7081
```

```bash
cd 14-OpenIddictAuthCode
dotnet run --project MvcClient       # https://localhost:7082
```

In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects**.

Open **https://localhost:7082**. Log in as `user` / `user` or `admin` / `admin`. The auth server's pages have a yellow background.

## What to try

1. Click **Secret** → you are on the auth server's login page (`localhost:7081`). Log in.
2. You are back in the app. **Secret** shows:
   - the claims: `sub` from the ID token, `name` and `IsAdmin` from the userinfo endpoint,
   - the decoded **ID token** (meant for the app),
   - the **access token** (meant for APIs; OpenIddict encrypts it, so the app can't read it).
3. **Admin**: *Access denied* for `user`, works for `admin`.
4. **Logout** logs you out of both the app and the auth server - **Secret** asks for the password again.

Tip: keep the browser's Network tab open (with "Preserve log") to watch the redirects: `/connect/authorize` → login page → back to `/signin-oidc` with a code.

## Key code - auth server

### Configuration (`AuthServer/Program.cs`)

```csharp
// The auth server's own login: plain cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.Cookie.Name = "AuthServer");

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AppDbContext>())
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout");

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();   // PKCE

        options.RegisterScopes(Scopes.Profile, "permissions");
        options.AddEphemeralSigningKey().AddEphemeralEncryptionKey();

        // Our controller handles these; the token endpoint (code exchange) is automatic
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    });
```

### Authorize endpoint (`AuthServer/Controllers/AuthorizationController.cs`)

OpenIddict has already checked the client, redirect URI, scopes and PKCE:

```csharp
[HttpGet("connect/authorize")]
public async Task<IActionResult> Authorize()
{
    var request = HttpContext.GetOpenIddictServerRequest()!;

    // Not logged in to the auth server? Show our login page, then come back here.
    var login = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!login.Succeeded)
        return Challenge(new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + Request.QueryString },
                         CookieAuthenticationDefaults.AuthenticationScheme);

    var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    identity.SetClaim(Claims.Subject, login.Principal.Identity!.Name);
    identity.SetScopes(request.GetScopes());
    identity.SetDestinations(_ => [Destinations.AccessToken, Destinations.IdentityToken]);

    // OpenIddict creates the code and redirects back to the client
    return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
```

### Userinfo endpoint - protected by the access token

```csharp
[Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
[HttpGet("connect/userinfo")]
public IActionResult UserInfo()
{
    var userName = User.GetClaim(Claims.Subject)!;
    var claims = new Dictionary<string, object> { [Claims.Subject] = userName };

    if (User.HasScope(Scopes.Profile))
        claims[Claims.Name] = userService.GetDisplayName(userName);
    if (User.HasScope("permissions"))
        claims["IsAdmin"] = userService.IsAdmin(userName) ? "true" : "false";

    return Ok(claims);
}
```

### Clients (`AuthServer/Data/SeedData.cs`)

```csharp
new OpenIddictApplicationDescriptor
{
    ClientId = "mvc",
    ClientSecret = "mvc-secret",
    ConsentType = ConsentTypes.Implicit,   // no "allow this app?" screen
    RedirectUris = { new Uri("https://localhost:7082/signin-oidc") },
    PostLogoutRedirectUris = { new Uri("https://localhost:7082/signout-callback-oidc") },
    Permissions =
    {
        Permissions.Endpoints.Authorization, Permissions.Endpoints.Token, Permissions.Endpoints.EndSession,
        Permissions.GrantTypes.AuthorizationCode, Permissions.ResponseTypes.Code,
        Permissions.Scopes.Profile, Permissions.Prefixes.Scope + "permissions"
    },
    Requirements = { Requirements.Features.ProofKeyForCodeExchange }
}
```

## Key code - MVC client

### Service configuration (`MvcClient/Program.cs`)

```csharp
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;      // remembers the user
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;    // logs them in
    })
    .AddCookie(options => options.Cookie.Name = "MvcClient")
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:7081";
        options.ClientId = "mvc";
        options.ClientSecret = "mvc-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;   // PKCE is on by default
        // ...
    });
```

Both apps run on `localhost`, and browsers don't separate cookies by port - so each app has its own cookie name.

### Signing in

```csharp
// Opening an [Authorize] page, or:
return Challenge(new AuthenticationProperties { RedirectUri = "/" });
```

1. The browser goes to the auth server's login page.
2. It comes back to `/signin-oidc` with a code.
3. The app exchanges the code (+ PKCE verifier) for tokens - server to server.
4. The claims are stored in the app's cookie.

### User info

```csharp
options.Scope.Add("permissions");                 // "openid" and "profile" are requested by default
options.GetClaimsFromUserInfoEndpoint = true;
options.ClaimActions.MapUniqueJsonKey("IsAdmin", "IsAdmin");   // userinfo values become claims only when mapped

options.MapInboundClaims = false;
options.TokenValidationParameters.NameClaimType = "name";
```

### Signing out

```csharp
return SignOut(new AuthenticationProperties { RedirectUri = "/" },
    CookieAuthenticationDefaults.AuthenticationScheme,
    OpenIdConnectDefaults.AuthenticationScheme);

options.SaveTokens = true;   // keeps the ID token, which is sent as a hint on logout
```

Without the `OpenIdConnect` scheme only the app's cookie is removed, and the next login would be silent.

## Project structure

| Project | What it does |
|---|---|
| `AuthServer` | OpenIddict server, login page, `AuthorizationController` (authorize, userinfo, logout), client registration |
| `MvcClient` | Cookie + OpenID Connect, Secret page with claims and tokens, Admin page |
