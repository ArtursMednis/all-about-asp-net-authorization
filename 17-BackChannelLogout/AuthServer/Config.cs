using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

namespace AuthServer;

public static class Config
{
    // Identity scopes: which information about the user a client can ask for.
    // Each one lists the user claims it unlocks (returned by the userinfo endpoint).
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),   // "openid": the user's id ("sub") - required
        new IdentityResources.Profile(),  // "profile": name, ...
        new IdentityResource("permissions", ["IsAdmin"]),
    ];

    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "mvc",
            ClientSecrets = { new Secret("mvc-secret".Sha256()) },

            // Authorization code flow. PKCE is required by default.
            AllowedGrantTypes = GrantTypes.Code,

            // Where the auth server may send the user back to. Must match exactly.
            RedirectUris = { "https://localhost:7102/signin-oidc" },
            PostLogoutRedirectUris = { "https://localhost:7102/signout-callback-oidc" },

            // When the user's session at the auth server ends, IdentityServer POSTs
            // a logout token here - server to server, the browser isn't involved.
            BackChannelLogoutUri = "https://localhost:7102/account/backchannel-logout",

            // Put the session id ("sid") into the logout token, so the client
            // knows exactly which session to end. (This is the default.)
            BackChannelLogoutSessionRequired = true,

            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "permissions"
            }
        },
    ];

    // Demo users kept in memory - the same accounts as in the other tutorials.
    // In a real app you'd plug in ASP.NET Core Identity (Duende.IdentityServer.AspNetIdentity).
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
