# 17 - Back-channel logout

When a user's session at the auth server ends, every app they logged in to through that session is told **server to server** - the browser isn't involved - and ends its own session too. Implements [OpenID Connect Back-Channel Logout](https://openid.net/specs/openid-connect-backchannel-1_0.html) with Duende IdentityServer and an MVC client.

> **License:** Duende IdentityServer is free for development and testing; production use requires a license. Without a license key it logs a warning at startup - expected here.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Two terminals:

```bash
cd 17-BackChannelLogout
dotnet run --project AuthServer      # https://localhost:7101
```

```bash
cd 17-BackChannelLogout
dotnet run --project MvcClient       # https://localhost:7102
```

Log in as `user` / `user` or `admin` / `admin`.

## What to try

1. Open https://localhost:7102, click **Secret** and log in. Note the session id (`sid`) on the page.
2. In a new tab open the auth server, https://localhost:7101, and click **Log out of all apps**.
3. The MvcClient console logs *Back-channel logout for user user, session ...* - the auth server just called it.
4. Back in the first tab, reload **Secret**: you have to log in again.

Without back-channel logout, step 4 would still show the page: the app's own cookie would still be valid.

## Key code

### Auth server (`AuthServer/Config.cs`)

```csharp
new Client
{
    ClientId = "mvc",
    // ...
    BackChannelLogoutUri = "https://localhost:7102/account/backchannel-logout",
    BackChannelLogoutSessionRequired = true,   // include "sid" in the logout token
}
```

Ending the session sends a logout token to every client of that session - automatically:

```csharp
await HttpContext.SignOutAsync();
```

### The logout token

`POST /account/backchannel-logout` with `logout_token=<signed JWT>`:

```json
{
  "iss": "https://localhost:7101",
  "aud": "mvc",
  "sub": "user",
  "sid": "A79C2FBB03F78E5B...",
  "events": {
    "http://schemas.openid.net/event/backchannel-logout": {}
  }
}
```

### Receiving it (`MvcClient/Controllers/BackChannelLogoutController.cs`)

Anyone can POST to this URL, so the token is validated like any other token - with the auth server's keys from the discovery document the OpenID Connect handler already downloaded:

```csharp
[HttpPost("account/backchannel-logout")]
[IgnoreAntiforgeryToken]   // server-to-server call
public async Task<IActionResult> Logout([FromForm(Name = "logout_token")] string logoutToken)
{
    var oidc = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
    var configuration = await oidc.ConfigurationManager!.GetConfigurationAsync(HttpContext.RequestAborted);

    var result = await new JsonWebTokenHandler().ValidateTokenAsync(logoutToken, new TokenValidationParameters
    {
        ValidIssuer = configuration.Issuer,
        IssuerSigningKeys = configuration.SigningKeys,
        ValidAudience = oidc.ClientId,
    });

    // + the spec's rules: must contain the logout event and a sid, and never a nonce
    if (!result.IsValid || !IsLogoutToken(result.ClaimsIdentity))
        return BadRequest();

    loggedOutSessions.Add(result.ClaimsIdentity.FindFirst("sid")!.Value);
    return Ok();
}
```

### Ending the session (`MvcClient/Program.cs`)

The cookie lives in the user's browser - the app can't delete it from the server. Instead it remembers the ended session ids and rejects their cookies on the next request:

```csharp
.AddCookie(options =>
{
    options.Cookie.Name = "MvcClient";
    options.Events.OnValidatePrincipal = async context =>
    {
        var sessionId = context.Principal?.FindFirst("sid")?.Value;
        var loggedOutSessions = context.HttpContext.RequestServices.GetRequiredService<LoggedOutSessions>();

        if (sessionId != null && loggedOutSessions.Contains(sessionId))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
})
```

`LoggedOutSessions` is in memory, which is fine for one app instance. With several instances use a shared store (Redis, a database), or keep sessions server-side (`ITicketStore`) and delete them directly.

The rest of the client is a standard OpenID Connect login (cookie + `AddOpenIdConnect` with `MapInboundClaims = false`, so the `sid` claim keeps its name).

## Project structure

| Path | What it does |
|---|---|
| `AuthServer/Config.cs` | Client with `BackChannelLogoutUri` |
| `AuthServer/Controllers/HomeController.cs`, `Views/Home/Index.cshtml` | Auth server home page with "Log out of all apps" |
| `AuthServer/Controllers/AccountController.cs` | Login and logout pages |
| `MvcClient/Controllers/BackChannelLogoutController.cs` | Receives and validates logout tokens |
| `MvcClient/Services/LoggedOutSessions.cs` | Ended session ids |
| `MvcClient/Program.cs` | OIDC login + the cookie check |
