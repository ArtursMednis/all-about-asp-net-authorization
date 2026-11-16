# 13 - OAuth 2.0 client credentials with Duende IdentityServer

Three applications, as OAuth 2.0 intends:

- an **authorization server** (built with [Duende IdentityServer](https://duendesoftware.com/products/identityserver)) that issues access tokens,
- a **resource API** that only validates tokens,
- a **console client** that gets a token with the *client credentials* flow and calls the API.

The API shares no secret with the auth server: it downloads the issuer and the public signing keys from the server's discovery document. IdentityServer builds tokens from configuration - there is no token endpoint code.

> **License:** Duende IdentityServer is free for development and testing; production use requires a license (a free Community Edition exists for small companies). Without a license key it logs a warning at startup - expected here.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`

## Run

Three terminals, in this order:

```bash
cd 13-DuendeClientCredentials
dotnet run --project AuthServer      # https://localhost:7071
```

```bash
cd 13-DuendeClientCredentials
dotnet run --project ResourceApi     # https://localhost:7072
```

```bash
cd 13-DuendeClientCredentials
dotnet run --project ConsoleClient
```

In Visual Studio: **Solution → Configure Startup Projects → Multiple startup projects**. The client pauses between steps - press Enter to continue.

| Client id | Secret | Allowed scopes |
|---|---|---|
| `client` | `client-secret` | `api` |
| `admin-client` | `admin-secret` | `api`, `admin` |

## What the client shows

1. **Discovery**: `/.well-known/openid-configuration` lists the issuer, token endpoint, signing keys and supported grant types.
2. No token → 401.
3. Wrong secret → `invalid_client`.
4. A token for `client` with the `api` scope.
5. **The token decoded**: `RS256`, `aud: resource_api`, `scope: ["api"]` (a JSON array), `client_id`.
6. `/api/secret` → 200.
7. `/api/admin` → 403 (no `admin` scope).
8. `client` asks for `admin` → `invalid_scope`.
9. `admin-client` with `api admin` → `/api/admin` 200.

## Key code

### Auth server (`AuthServer/Program.cs`)

```csharp
builder.Services.AddIdentityServer(options =>
    {
        // Automatic key management would store keys in a "keys" folder
        options.KeyManagement.Enabled = false;
    })
    .AddDeveloperSigningCredential(persistKey: false)   // in-memory key, demo only
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients);

app.UseIdentityServer();   // discovery, token, ... endpoints
```

### Clients (`AuthServer/Config.cs`)

```csharp
new Client
{
    ClientId = "admin-client",
    ClientSecrets = { new Secret("admin-secret".Sha256()) },
    AllowedGrantTypes = GrantTypes.ClientCredentials,
    AllowedScopes = { "api", "admin" }
}
```

### Scopes and APIs

```csharp
new ApiScope("api"),
new ApiScope("admin"),

new ApiResource("resource_api")   // the name becomes the token's "aud" claim
{
    Scopes = { "api", "admin" }
}
```

### Resource API (`ResourceApi/Program.cs`)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7071";   // issuer + signing keys via discovery
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
| `AuthServer` | Duende IdentityServer with in-memory clients, scopes and API resources (`Config.cs`) |
| `ResourceApi` | JwtBearer with `Authority` + `Audience`, `/api/secret`, `/api/admin` |
| `ConsoleClient` | Discovery, token requests, API calls - 9 steps |
