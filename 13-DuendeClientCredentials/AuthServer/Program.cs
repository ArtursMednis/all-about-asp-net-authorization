using AuthServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServer(options =>
    {
        // Automatic key management stores keys in a "keys" folder. We use a temporary key instead.
        options.KeyManagement.Enabled = false;
    })
    // A new signing key on every start, kept in memory. Demo only - use a real certificate in production.
    .AddDeveloperSigningCredential(persistKey: false)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients);

var app = builder.Build();

// Adds the OAuth 2.0 / OpenID Connect endpoints: discovery, token, ...
// No controller needed: IdentityServer builds the token from Config.cs.
app.UseIdentityServer();

app.Run();
