# 10 - Token authentication (JWT)

An API protected with **JWT bearer tokens**. A token endpoint works like the OAuth 2.0 *client credentials* flow: an application sends its client id and secret and gets back a signed access token. Every API call carries it in the `Authorization: Bearer` header, and the API validates it properly: **signature, issuer, audience and lifetime**. Permissions are **scopes**.

A console app in the same solution walks through every case.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

Start the API, then the client (two terminals):

```bash
cd 10-TokenAuth
dotnet run --project TokenAuthApi
```

```bash
cd 10-TokenAuth
dotnet run --project TokenAuthClient
```

In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects**, API first. The API listens on http://localhost:5005. The client pauses between steps - press Enter to continue.

| Client id | Secret | Allowed scopes |
|---|---|---|
| `client` | `client-secret` | none |
| `admin-client` | `admin-secret` | `admin` |

## What the client shows

1. No token → 401.
2. Wrong secret → `invalid_client`.
3. A token for `client`.
4. **The token decoded** - a JWT is only signed, not encrypted: anyone can read it.
5. The token on `/api/secret` → 200 with the claims.
6. The token on `/api/admin` → 403 (no `admin` scope).
7. `client` asks for the `admin` scope → `invalid_scope`.
8. `admin-client` with the `admin` scope → `/api/admin` 200.
9. One character of the token changed → 401, *signature invalid* (see the `WWW-Authenticate` header).

## Key code

### Service configuration (`TokenAuthApi/Program.cs`)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)   // "Bearer"
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;   // keep "sub", "scope" as they are
        options.TokenValidationParameters = new TokenValidationParameters { ... };
    });

app.UseAuthentication();
```

No token or an invalid token → 401, no redirect.

### Validating tokens

```csharp
new TokenValidationParameters
{
    // Signed with our key?
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = jwt.SigningKey,

    // Created by us, for us?
    ValidateIssuer = true,   ValidIssuer = jwt.Issuer,
    ValidateAudience = true, ValidAudience = jwt.Audience,

    // Not expired? (the default ClockSkew allows 5 extra minutes)
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,

    NameClaimType = "sub",   // what User.Identity.Name reads
};
```

The settings come from the `Jwt` section of `appsettings.json`. The signing key is there only for the demo - in a real app keep it in user secrets, an environment variable or a key vault.

### Issuing a token (`TokenAuthApi/Controllers/TokenController.cs`)

`POST /connect/token` with `grant_type=client_credentials`, `client_id`, `client_secret`, `scope`:

```csharp
// The client may only get scopes it is allowed to have
if (requestedScopes.Except(clientService.GetAllowedScopes(clientId)).Any())
    return BadRequest(new { error = "invalid_scope" });

claims.Add(new Claim("scope", string.Join(' ', requestedScopes)));

var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Issuer = jwt.Issuer,
    Audience = jwt.Audience,
    Expires = DateTime.UtcNow.AddMinutes(jwt.DurationInMinutes),
    SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
});

return Ok(new { access_token = token, token_type = "Bearer", expires_in = ..., scope = ... });
```

### Client storage (`TokenAuthApi/Services/ClientService.cs`)

```csharp
clientService.VerifySecret(clientId, clientSecret)   // secrets are stored hashed
clientService.GetAllowedScopes(clientId)
```

### Authorization by scope

```csharp
// "scope" holds space-separated values, e.g. "admin other"
options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
    context.User.FindFirst("scope")?.Value.Split(' ').Contains("admin") == true));

[Authorize(Policy = "AdminOnly")]
[HttpGet("api/admin")]
```

Valid token without the `admin` scope → 403.

### Using the token (`TokenAuthClient/Program.cs`)

```csharp
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

## Project structure

| File | What it does |
|---|---|
| `TokenAuthApi/Program.cs` | JwtBearer with full validation, scope policy |
| `TokenAuthApi/Controllers/TokenController.cs` | Token endpoint (client credentials) |
| `TokenAuthApi/Controllers/ResourceController.cs` | `/api/secret`, `/api/admin` |
| `TokenAuthApi/Services/JwtSettings.cs` | Issuer, audience, key, lifetime (from appsettings.json) |
| `TokenAuthApi/Services/ClientService.cs` | Clients with hashed secrets and allowed scopes |
| `TokenAuthClient/Program.cs` | Console client: 9 steps |
