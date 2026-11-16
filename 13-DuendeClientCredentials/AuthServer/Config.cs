using Duende.IdentityServer.Models;

namespace AuthServer;

// Everything IdentityServer needs to know, defined in code.
// In a real app this usually comes from a database (Duende.IdentityServer.EntityFramework).
public static class Config
{
    // Scopes: the permissions a client can ask for.
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("api"),
        new ApiScope("admin"),
    ];

    // APIs, and which scopes belong to them.
    // The API name ends up in the token's "aud" (audience) claim.
    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("resource_api")
        {
            Scopes = { "api", "admin" }
        },
    ];

    // Clients (applications), with what each one is allowed to do.
    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "client",
            ClientSecrets = { new Secret("client-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" }
        },
        new Client
        {
            ClientId = "admin-client",
            ClientSecrets = { new Secret("admin-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api", "admin" }
        },
    ];
}
