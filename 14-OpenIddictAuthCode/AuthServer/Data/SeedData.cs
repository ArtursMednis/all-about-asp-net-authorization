using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Data;

public static class SeedData
{
    public static async Task SeedClientsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "mvc",
            ClientSecret = "mvc-secret",

            // Skip the "Allow this app to access your data?" screen.
            ConsentType = ConsentTypes.Implicit,

            // Where the auth server may send the user back to. Must match exactly.
            RedirectUris = { new Uri("https://localhost:7082/signin-oidc") },
            PostLogoutRedirectUris = { new Uri("https://localhost:7082/signout-callback-oidc") },

            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Profile,
                Permissions.Prefixes.Scope + "permissions"
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }
}
