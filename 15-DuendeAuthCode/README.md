# 15 - Authorization code flow with Duende IdentityServer (OpenID Connect login)

Users log in to an MVC app through a separate **auth server** - the app never sees a password. The flow is the OAuth 2.0 **authorization code flow with PKCE**, plus **OpenID Connect** to tell the app who logged in. The auth server is [Duende IdentityServer](https://duendesoftware.com/products/identityserver): authorize, token, userinfo and end-session endpoints are built in - the only code we write is the login and logout page.

> **License:** Duende IdentityServer is free for development and testing; production use requires a license (a free Community Edition exists for small companies). Without a license key it logs a warning at startup - expected here.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Two terminals:

```bash
cd 15-DuendeAuthCode
dotnet run --project AuthServer      # https://localhost:7091
```

```bash
cd 15-DuendeAuthCode
dotnet run --project MvcClient       # https://localhost:7092
```

In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects**.

Open **https://localhost:7092**. Log in as `user` / `user` or `admin` / `admin`. The auth server's pages have a yellow background.

## What to try

1. Click **Secret** → you are on the auth server's login page (`localhost:7091`). Log in.
2. The authorize URL in the address bar is short (`client_id` + `request_uri`): that's **PAR** (pushed authorization requests). ASP.NET Core uses it automatically because IdentityServer supports it.
3. Back in the app, **Secret** shows the claims (`sub`, `sid`, `name`, `IsAdmin`, ...), the decoded **ID token** and the **access token**.
4. **Admin**: *Access denied* for `user`, works for `admin`.
5. **Logout** logs you out of both the app and the auth server.

## Key code - auth server

### Configuration (`AuthServer/Program.cs`)

```csharp
builder.Services.AddControllersWithViews();   // for the login page

builder.Services.AddIdentityServer(options =>
    {
        options.KeyManagement.Enabled = false;   // use the in-memory key below
        options.UserInteraction.LoginUrl = "/Account/Login";
        options.UserInteraction.LogoutUrl = "/Account/Logout";
    })
    .AddDeveloperSigningCredential(persistKey: false)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryClients(Config.Clients)
    .AddTestUsers(Config.Users);   // users + a profile service for userinfo

app.UseIdentityServer();   // all OAuth/OIDC endpoints, and IdentityServer's own login cookie
```

### Login page (`AuthServer/Controllers/AccountController.cs`)

```csharp
if (!users.ValidateCredentials(userName, password))
    return View();

var user = users.FindByUsername(userName);
await HttpContext.SignInAsync(new IdentityServerUser(user.SubjectId) { DisplayName = user.Username });

// returnUrl continues the authorize request; IsValidReturnUrl rejects anything else
return interaction.IsValidReturnUrl(returnUrl) ? Redirect(returnUrl!) : Content("Logged in to the auth server.");
```

Logout: IdentityServer sends the user to `/Account/Logout?logoutId=...`:

```csharp
var context = await interaction.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);
await HttpContext.SignOutAsync();
return Redirect(context.PostLogoutRedirectUri ?? "/");
```

### Identity scopes and users (`AuthServer/Config.cs`)

```csharp
new IdentityResources.OpenId(),                    // "sub"
new IdentityResources.Profile(),                   // "name", ...
new IdentityResource("permissions", ["IsAdmin"]),  // our own scope

new TestUser
{
    SubjectId = "admin", Username = "admin", Password = "admin",
    Claims = { new Claim("name", "Administrator"), new Claim("IsAdmin", "true") }
}
```

The userinfo endpoint returns the claims of the scopes the client was granted.

### Clients

```csharp
new Client
{
    ClientId = "mvc",
    ClientSecrets = { new Secret("mvc-secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.Code,   // PKCE is required by default
    RedirectUris = { "https://localhost:7092/signin-oidc" },
    PostLogoutRedirectUris = { "https://localhost:7092/signout-callback-oidc" },
    AllowedScopes = { "openid", "profile", "permissions" }
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
        options.Authority = "https://localhost:7091";
        options.ClientId = "mvc";
        options.ClientSecret = "mvc-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;
        // ...
    });
```

Both apps run on `localhost`, and browsers don't separate cookies by port - so the app has its own cookie name.

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
| `AuthServer` | Duende IdentityServer, `Config.cs` (identity scopes, client, test users), login/logout page |
| `MvcClient` | Cookie + OpenID Connect, Secret page with claims and tokens, Admin page |
