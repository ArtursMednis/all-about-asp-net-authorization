using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Data;

public static class SeedData
{
    public static async Task SeedClientsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Scopes, and the API ("resource") each one gives access to.
        // The resource ends up in the token's "aud" (audience) claim.
        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "api",
            Resources = { "resource_api" }
        });
        await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "admin",
            Resources = { "resource_api" }
        });

        // Clients (applications), with what each one is allowed to do.
        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "client",
            ClientSecret = "client-secret", // OpenIddict stores it hashed
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + "api"
            }
        });
        await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "admin-client",
            ClientSecret = "admin-secret",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + "api",
                Permissions.Prefixes.Scope + "admin"
            }
        });
    }
}
