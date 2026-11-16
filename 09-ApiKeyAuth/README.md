# 09 - Custom authentication handler: API keys

A complete custom authentication scheme: callers send an API key in an `X-Api-Key` header, and our own `AuthenticationHandler` turns it into a logged-in user. Shows how every authentication handler (cookies, JWT bearer, ...) works inside: **authenticate**, **challenge** and **forbid**. Authorization (`[Authorize]`, policies) then works unchanged.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 09-ApiKeyAuth
dotnet run --project ApiKeyAuth
```

The API listens on http://localhost:5209. Send requests with `ApiKeyAuth/ApiKeyAuth.http` (Visual Studio, Rider, or VS Code with the REST Client extension), or with curl:

```bash
curl -i http://localhost:5209/api/secret
curl -i -H "X-Api-Key: key-for-client" http://localhost:5209/api/secret
curl -i -H "X-Api-Key: key-for-admin-client" http://localhost:5209/api/admin
```

| API key | Client | |
|---|---|---|
| `key-for-client` | `client` | regular client |
| `key-for-admin-client` | `admin-client` | has the `IsAdmin` claim |

## What to try

| Request | Result |
|---|---|
| no key | 401, `WWW-Authenticate: ApiKey header="X-Api-Key"` |
| wrong key | 401, with `error="Invalid API key"` |
| `key-for-client` → `/api/secret` | 200, `authenticationType` is `ApiKey` |
| `key-for-client` → `/api/admin` | 403 |
| `key-for-admin-client` → `/api/admin` | 200 |

## Key code

### Service configuration (`Program.cs`)

```csharp
builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddApiKey(options => options.HeaderName = "X-Api-Key");
```

`AddApiKey` is our own extension method - the same way `AddCookie` and `AddJwtBearer` are built:

```csharp
public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder, Action<ApiKeyOptions>? configure = null)
{
    return builder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, configure);
}

public class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
}
```

### Authenticate (`Authentication/ApiKeyAuthenticationHandler.cs`)

"Who is calling?" - runs on every request:

```csharp
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    // No key: not an error, just anonymous. [Authorize] decides whether that's OK.
    if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKey))
        return Task.FromResult(AuthenticateResult.NoResult());

    var client = apiKeyStore.Find(apiKey.ToString());
    if (client == null)
        return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

    // claims -> identity -> principal, as always
    var claims = new List<Claim> { new(ClaimTypes.Name, client.Name) };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
}
```

### Challenge and forbid

"Please authenticate" - runs when `[Authorize]` fails and nobody is logged in:

```csharp
protected override Task HandleChallengeAsync(AuthenticationProperties properties)
{
    Response.StatusCode = StatusCodes.Status401Unauthorized;
    Response.Headers.WWWAuthenticate = $"ApiKey header=\"{Options.HeaderName}\"";
    return Task.CompletedTask;
}

// "Logged in, but not allowed": HandleForbiddenAsync keeps its default -> 403
```

### Storing keys (`Services/ApiKeyStore.cs`)

Like passwords, API keys are stored only as hashes:

```csharp
[Hash("key-for-client")] = new ApiClient("client", IsAdmin: false),

private static string Hash(string apiKey) =>
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
```

A plain SHA-256 is fine for long random keys; passwords need a slow hash (like `PasswordHasher`).

### Authorization - unchanged

```csharp
[Authorize]
[HttpGet("api/secret")]

[Authorize(Policy = "AdminOnly")]   // policy.RequireClaim("IsAdmin", "true")
[HttpGet("api/admin")]
```

## Project structure

| File | What it does |
|---|---|
| `Authentication/ApiKeyAuthenticationHandler.cs` | Options, handler, `AddApiKey` extension |
| `Services/ApiKeyStore.cs` | Hashed API keys and their clients |
| `Program.cs` | Registers the scheme and the `AdminOnly` policy |
| `Controllers/ResourceController.cs` | Protected endpoints |
| `ApiKeyAuth.http` | Ready-to-run requests |
