using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Two schemes work together:
//  - Cookies:        remembers the user in this app, exactly like tutorial 01.
//  - OpenIdConnect:  when a login is needed, sends the user to the auth server
//                    and turns the response into that cookie.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        // Both apps run on localhost, and browsers don't separate cookies by port.
        options.Cookie.Name = "MvcClient";
    })
    .AddOpenIdConnect(options =>
    {
        // The auth server. Endpoints and signing keys come from its discovery document.
        options.Authority = "https://localhost:7081";

        // Who we are, as registered on the auth server.
        options.ClientId = "mvc";
        options.ClientSecret = "mvc-secret";

        // Authorization code flow (PKCE is on by default).
        options.ResponseType = OpenIdConnectResponseType.Code;

        // "openid" and "profile" are requested by default.
        options.Scope.Add("permissions");

        // After login, call the userinfo endpoint with the access token
        // to get the user's name and permissions.
        options.GetClaimsFromUserInfoEndpoint = true;

        // Userinfo values are only copied into claims when mapped.
        // "name" is mapped by default, our own "IsAdmin" is not.
        options.ClaimActions.MapUniqueJsonKey("IsAdmin", "IsAdmin");

        // Keep the tokens in the cookie: we show them on the Secret page,
        // and the ID token is needed to log out of the auth server.
        options.SaveTokens = true;

        // Keep claim names as they are ("sub", "name") and use "name" for User.Identity.Name.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
