using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

namespace AuthServer;

public static class Config
{
    // What the client may know about the user (ID token / userinfo).
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("permissions", ["IsAdmin"]),
    ];

    // What the client may do: call the resource API.
    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("api"),
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("resource_api")
        {
            Scopes = { "api" },

            // User claims copied into access tokens for this API,
            // so the API can authorize with the same AdminOnly policy as tutorial 01.
            UserClaims = { "name", "IsAdmin" }
        },
    ];

    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            // The BFF is a normal confidential web app - the SPA itself is not a client.
            ClientId = "bff",
            ClientSecrets = { new Secret("bff-secret".Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,

            RedirectUris = { "https://localhost:7122/signin-oidc" },
            PostLogoutRedirectUris = { "https://localhost:7122/signout-callback-oidc" },

            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "permissions",
                "api"
            },

            // Allow the "offline_access" scope: the BFF gets a refresh token.
            AllowOfflineAccess = true,

            // Very short on purpose, so you can watch the BFF refresh it. The default is 1 hour.
            AccessTokenLifetime = 30,

            // The same refresh token can be used again. Fine for a confidential client like the BFF,
            // and it avoids failures when two requests refresh at the same moment.
            // (OneTimeOnly = rotation, a new refresh token every time.)
            RefreshTokenUsage = TokenUsage.ReUse,
        },
    ];

    public static List<TestUser> Users =>
    [
        new TestUser
        {
            SubjectId = "user",
            Username = "user",
            Password = "user",
            Claims = { new Claim("name", "Regular User") }
        },
        new TestUser
        {
            SubjectId = "admin",
            Username = "admin",
            Password = "admin",
            Claims = { new Claim("name", "Administrator"), new Claim("IsAdmin", "true") }
        },
    ];
}
