# 16 - Backend for Frontend (BFF) with token refresh

A React single-page app that calls a token-protected API **without any tokens in the browser**. A small ASP.NET Core **Backend for Frontend** logs the user in with OpenID Connect, keeps the tokens server-side inside its encrypted session cookie, forwards API calls with the access token attached (using [YARP](https://microsoft.github.io/reverse-proxy/)), and **refreshes the access token** with a refresh token when it expires.

| Project | URL | Role |
|---|---|---|
| `AuthServer` | https://localhost:7121 | Duende IdentityServer - issues ID, access and refresh tokens |
| `ResourceApi` | https://localhost:7123 | Accepts access tokens only |
| `Bff` | **https://localhost:7122** | Login, token storage, reverse proxy, token refresh - **open this one** |
| `react-client` | http://localhost:5122 | Vite dev server (the BFF forwards to it) |

> **License:** Duende IdentityServer is free for development and testing; production use requires a license. Without a license key it logs a warning at startup - expected here.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 or newer
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Four terminals:

```bash
cd 16-Bff
dotnet run --project AuthServer
```

```bash
cd 16-Bff
dotnet run --project ResourceApi
```

```bash
cd 16-Bff
dotnet run --project Bff
```

```bash
cd 16-Bff/react-client
npm install
npm run dev
```

Open **https://localhost:7122** (not the Vite port). Log in as `user` / `user` or `admin` / `admin`.

In development the BFF forwards every non-API request to the Vite dev server, so the app and the BFF share one origin. For production, `npm run build` writes the app to `Bff/wwwroot`, and the BFF serves it itself.

## What to try

1. Click **Secret** → log in on the auth server → back on Secret, with data from the resource API.
2. Developer tools → **Cookies**: one `Bff` cookie, HttpOnly. `document.cookie` is empty - no token for JavaScript to steal.
3. Network tab: the SPA called `/api/secret` on its own origin with a cookie and an `X-CSRF: 1` header - no `Authorization` header in the browser.
4. Access tokens live only **30 seconds** (on purpose). Wait, then click **Call the API again**: the expiry time moves forward, and the BFF's console logs *Refreshed the access token*.
5. **Admin** as `user` → 403 from the resource API; as `admin` → 200.

## Key code - BFF

### Login (`Bff/Program.cs`)

```csharp
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "Bff";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnValidatePrincipal = context =>
            context.HttpContext.RequestServices.GetRequiredService<TokenRefresher>().RefreshIfNeededAsync(context);
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:7121";
        options.ClientId = "bff";
        options.ClientSecret = "bff-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Add("api");              // call the resource API
        options.Scope.Add("offline_access");   // and get a refresh token
        options.SaveTokens = true;             // tokens stay inside the encrypted cookie
        // ...
    });
```

The SPA logs in and out with page navigations (`Bff/Controllers/BffController.cs`):

```csharp
[HttpGet("bff/login")]    // -> Challenge, back to returnUrl
[HttpGet("bff/user")]     // who is logged in? 401 instead of a redirect
[HttpGet("bff/logout")]   // ?sid=... -> sign out of the BFF and the auth server
```

### Proxy with token

```csharp
var routes = new List<RouteConfig>
{
    new() { RouteId = "api", ClusterId = "api", Match = new() { Path = "/api/{**rest}" } },
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transform =>
        {
            var accessToken = await transform.HttpContext.GetTokenAsync("access_token");
            if (accessToken != null)
                transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        });
    });

app.MapReverseProxy();
```

The browser sends a cookie, the API receives a token.

### Token refresh (`Bff/Services/TokenRefresher.cs`)

Runs on every request that has the session cookie:

```csharp
// Still valid for a while? Nothing to do.
if (DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture) > DateTimeOffset.UtcNow.AddSeconds(5))
    return;

// POST to the token endpoint
var response = await httpClient.PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["grant_type"] = "refresh_token",
    ["refresh_token"] = refreshToken,
    ["client_id"] = oidc.ClientId!,
    ["client_secret"] = oidc.ClientSecret!,
}));

// Store the new tokens and write the cookie again
context.Properties.UpdateTokenValue("access_token", ...);
context.Properties.UpdateTokenValue("expires_at", ...);
context.ShouldRenew = true;
```

If the refresh fails, the session ends and the user logs in again. (For production, the open-source `Duende.AccessTokenManagement.OpenIdConnect` library does this for you.)

### CSRF protection

```csharp
// The SPA adds this header to every fetch:
fetch(url, { headers: { 'X-CSRF': '1' } })

// The BFF rejects /api and /bff/user requests without it -> 400
if ((path.StartsWithSegments("/api") || path.StartsWithSegments("/bff/user"))
    && context.Request.Headers["X-CSRF"] != "1") { ... }
```

Another site can't add a custom header without a CORS preflight, which the BFF never allows. Logout is a navigation, so it can't send a header - instead its URL must contain the session id (`/bff/logout?sid=...`), which another site can't know.

## Key code - the other projects

### Auth server client (`AuthServer/Config.cs`)

```csharp
new Client
{
    ClientId = "bff",
    ClientSecrets = { new Secret("bff-secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.Code,
    AllowOfflineAccess = true,              // refresh tokens
    AccessTokenLifetime = 30,               // seconds - only for the demo
    RefreshTokenUsage = TokenUsage.ReUse,   // fine for a confidential client
    AllowedScopes = { "openid", "profile", "permissions", "api" },
    // RedirectUris, PostLogoutRedirectUris ...
}

new ApiResource("resource_api")
{
    Scopes = { "api" },
    UserClaims = { "name", "IsAdmin" }   // user claims inside the access token
}
```

The BFF is the OAuth client - the SPA is not.

### Resource API (`ResourceApi/Program.cs`)

```csharp
.AddJwtBearer(options =>
{
    options.Authority = "https://localhost:7121";
    options.Audience = "resource_api";
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;   // 30 seconds really means 30 seconds
});

options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
```

It knows nothing about the BFF or the browser.

### React client (`react-client/src`)

```ts
// Login and logout are page navigations
window.location.href = `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`
<a href={user.logoutUrl}>Logout</a>

// Who am I?
await api('/bff/user')
```

## Project structure

| Path | What it does |
|---|---|
| `AuthServer` | Duende IdentityServer: `bff` client with refresh tokens, API resource, login page |
| `ResourceApi` | Token-protected API (`/api/secret` shows the token's expiry) |
| `Bff/Program.cs` | OIDC login, YARP proxy with token, CSRF check, dev forwarding to Vite |
| `Bff/Services/TokenRefresher.cs` | Refresh-token logic |
| `Bff/Controllers/BffController.cs` | `/bff/login`, `/bff/user`, `/bff/logout` |
| `react-client` | React + React Router + Vite, no tokens |
