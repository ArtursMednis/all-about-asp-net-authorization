using AuthServer.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenIddict keeps clients and scopes in a database through EF Core.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("AuthServer");
    options.UseOpenIddict(); // adds OpenIddict's tables (applications, scopes, tokens, ...)
});

builder.Services.AddOpenIddict()
    // Core: where clients and scopes are stored.
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
    })
    // Server: the OAuth 2.0 / OpenID Connect endpoints.
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("connect/token");
        options.AllowClientCredentialsFlow();

        // Keys used to sign (and encrypt) tokens. Ephemeral = new keys on every start, demo only.
        // In production use real certificates: AddSigningCertificate / AddEncryptionCertificate.
        options.AddEphemeralSigningKey()
               .AddEphemeralEncryptionKey();

        // OpenIddict encrypts access tokens by default. We turn that off so they are plain
        // signed JWTs that any API can validate with standard JwtBearer.
        options.DisableAccessTokenEncryption();

        // Let our own TokenController decide what goes into the token.
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough();
    });

var app = builder.Build();

await app.SeedClientsAsync();

app.UseAuthentication(); // OpenIddict handles its endpoints from here
app.UseAuthorization();

app.MapControllers();

app.Run();
