# 12 - OAuth 2.0 client credentials with OpenIddict

Three applications, as OAuth 2.0 intends:

- an **authorization server** (built with [OpenIddict](https://documentation.openiddict.com/)) that issues access tokens,
- a **resource API** that only validates tokens,
- a **console client** that gets a token with the *client credentials* flow and calls the API.

The API shares no secret with the auth server: it downloads the issuer and the public signing keys from the server's discovery document.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Three terminals, in this order:

```bash
cd 12-OpenIddictClientCredentials
dotnet run --project AuthServer      # https://localhost:7061
```

```bash
cd 12-OpenIddictClientCredentials
dotnet run --project ResourceApi     # https://localhost:7062
```

```bash
cd 12-OpenIddictClientCredentials
dotnet run --project ConsoleClient
```

In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects**. The client pauses between steps - press Enter to continue.

| Client id | Secret | Allowed scopes |
|---|---|---|
| `client` | `client-secret` | `api` |
| `admin-client` | `admin-secret` | `api`, `admin` |

## What the client shows

1. **Discovery**: `/.well-known/openid-configuration` lists the issuer, token endpoint and signing keys.
2. No token → 401.
3. Wrong secret → `invalid_client`.
4. A token for `client` with the `api` scope.
5. **The token decoded**: `RS256` (signed with the server's private key), `aud: resource_api`, `scope: "api"`.
6. `/api/secret` → 200.
7. `/api/admin` → 403 (no `admin` scope).
8. `client` asks for `admin` → refused.
9. `admin-client` with `api admin` → `/api/admin` 200.

## Key code

### Auth server (`AuthServer/Program.cs`)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("AuthServer");
    options.UseOpenIddict();   // adds OpenIddict's tables
});

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AppDbContext>())
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token");
        options.AllowClientCredentialsFlow();

        // New keys on every start - demo only, use certificates in production
        options.AddEphemeralSigningKey().AddEphemeralEncryptionKey();

        // Plain signed JWTs that any API can validate with JwtBearer
        options.DisableAccessTokenEncryption();

        // Our TokenController decides what goes into the token
        options.UseAspNetCore().EnableTokenEndpointPassthrough();
    });

app.UseAuthentication();
```

### Issuing a token (`AuthServer/Controllers/TokenController.cs`)

OpenIddict has already checked the client secret, the grant type and the allowed scopes:

```csharp
var request = HttpContext.GetOpenIddictServerRequest()!;

var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
identity.SetClaim(Claims.Subject, request.ClientId);   // no user - the client is the subject
identity.SetScopes(request.GetScopes());
identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());   // -> "aud"
identity.SetDestinations(_ => [Destinations.AccessToken]);

return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
```

### Clients and scopes (`AuthServer/Data/SeedData.cs`)

```csharp
await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
{
    Name = "admin",
    Resources = { "resource_api" }   // the API this scope gives access to
});

await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
{
    ClientId = "admin-client",
    ClientSecret = "admin-secret",   // stored hashed
    Permissions =
    {
        Permissions.Endpoints.Token,
        Permissions.GrantTypes.ClientCredentials,
        Permissions.Prefixes.Scope + "api",
        Permissions.Prefixes.Scope + "admin"
    }
});
```

### Resource API (`ResourceApi/Program.cs`)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7061";   // issuer + signing keys via discovery
        options.Audience = "resource_api";              // only tokens for this API
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "client_id";
    });

// Scopes can arrive as one space-separated claim or as several claims
options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
    context.User.FindAll("scope").Any(c => c.Value.Split(' ').Contains("admin"))));
```

### Console client (`ConsoleClient/Program.cs`)

```csharp
// Discovery
var discovery = await http.GetFromJsonAsync<JsonElement>($"{AuthServer}/.well-known/openid-configuration");
var tokenEndpoint = discovery.GetProperty("token_endpoint").GetString();

// Client credentials request
var response = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["grant_type"] = "client_credentials",
    ["client_id"] = "client",
    ["client_secret"] = "client-secret",
    ["scope"] = "api",
}));

request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
```

## Project structure

| Project | What it does |
|---|---|
| `AuthServer` | OpenIddict server, token endpoint (`TokenController`), clients and scopes in EF Core (in-memory) |
| `ResourceApi` | JwtBearer with `Authority` + `Audience`, `/api/secret`, `/api/admin` |
| `ConsoleClient` | Discovery, token requests, API calls - 9 steps |
