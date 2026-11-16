using AuthServer.Data;
using AuthServer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();

// The auth server's own login - plain cookie authentication from tutorial 01.
// Users log in here, never in the client app.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Both apps run on localhost, and browsers don't separate cookies by port.
        options.Cookie.Name = "AuthServer";
    });

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("AuthServer");
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore().UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("connect/authorize")
               .SetTokenEndpointUris("connect/token")
               .SetUserInfoEndpointUris("connect/userinfo")
               .SetEndSessionEndpointUris("connect/logout");

        // Authorization code flow, with PKCE required (protects the code if it's stolen).
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // Scopes clients may ask for, besides "openid".
        options.RegisterScopes(Scopes.Profile, "permissions");

        options.AddEphemeralSigningKey()
               .AddEphemeralEncryptionKey();

        // Our own controllers handle these endpoints (AuthorizationController).
        // The token endpoint is fully handled by OpenIddict: it exchanges the code for tokens.
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    });

var app = builder.Build();

await app.SeedClientsAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
