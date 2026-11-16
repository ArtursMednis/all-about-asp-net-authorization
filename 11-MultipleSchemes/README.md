# 11 - Multiple authentication schemes in one app

One app with two kinds of callers: **people** use the web pages and log in with a **cookie**, **apps** call the API with a **JWT bearer token**. A **policy scheme** picks the right handler for each request, and an endpoint can also require one specific scheme. Policies work the same for both.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 11-MultipleSchemes
dotnet run --project MultipleSchemes
```

Open http://localhost:5211. Users: `user` / `user` and `admin` / `admin` (has the `IsAdmin` claim).

For bearer-token calls use `MultipleSchemes/MultipleSchemes.http` (Visual Studio, Rider, or VS Code with REST Client), or curl:

```bash
curl -X POST http://localhost:5211/api/token -H "Content-Type: application/json" -d "{\"userName\":\"admin\",\"password\":\"admin\"}"
curl http://localhost:5211/api/secret -H "Authorization: Bearer <access_token>"
```

## What to try

| Caller | `/api/secret` | `/api/bearer-only` | `/api/admin` (as admin) |
|---|---|---|---|
| Browser, logged in (buttons on the **Secret** page) | 200, by `Cookies` | 401 | 200 |
| Bearer token (`.http` file) | 200, by `Bearer` | 200 | 200 |
| Nothing | 401 | 401 | 401 |

Web pages without a login redirect to the login form; API endpoints answer 401 instead.

## Key code

### Service configuration (`Program.cs`)

```csharp
builder.Services.AddAuthentication("CookieOrBearer")

    // Doesn't authenticate anything itself - picks a real scheme for each request
    .AddPolicyScheme("CookieOrBearer", "Cookie or bearer token", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString().StartsWith("Bearer ")
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })

    .AddCookie()

    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, IssuerSigningKey = jwt.SigningKey,
            ValidateIssuer = true,           ValidIssuer = jwt.Issuer,
            ValidateAudience = true,         ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            NameClaimType = "name",
            AuthenticationType = JwtBearerDefaults.AuthenticationScheme,   // shows as "Bearer"
        };
    });
```

### Default scheme - both kinds of callers (`Controllers/ApiController.cs`)

```csharp
[Authorize]
[HttpGet("secret")]
public IActionResult Secret() => Ok(new
{
    message = $"Hello, {User.Identity!.Name}",
    authenticatedBy = User.Identity.AuthenticationType,   // "Cookies" or "Bearer"
});
```

### One scheme only

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[HttpGet("bearer-only")]
```

The browser's cookie is ignored here. The same works in a policy:

```csharp
options.AddPolicy("ApiOnly", policy => policy
    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser());
```

### Policies are scheme-independent

```csharp
options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));

[Authorize(Policy = "AdminOnly")]
[HttpGet("admin")]
```

The cookie and the token both carry `IsAdmin` for the admin, so one policy covers both.

### Getting a token

`POST /api/token` with user name and password returns a JWT with `name` and `IsAdmin` claims, signed with the key from the `Jwt` section of `appsettings.json` (demo only - keep real keys out of source control).

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Policy scheme, cookie, JwtBearer, `AdminOnly` policy |
| `Controllers/ApiController.cs` | Token endpoint and API endpoints |
| `Controllers/AccountController.cs` | Cookie login for the web pages |
| `Views/Home/Secret.cshtml` | Buttons that call the API with the cookie |
| `MultipleSchemes.http` | Bearer-token requests |
