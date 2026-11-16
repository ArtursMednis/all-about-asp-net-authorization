using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MvcClient.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LoggedOutSessions>();

// The same setup as tutorial 15, plus one cookie event (below).
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "MvcClient";

        // Runs on every request that has our cookie.
        // If the auth server told us this session has ended, reject the cookie.
        options.Events.OnValidatePrincipal = async context =>
        {
            var sessionId = context.Principal?.FindFirst("sid")?.Value;
            var loggedOutSessions = context.HttpContext.RequestServices.GetRequiredService<LoggedOutSessions>();

            if (sessionId != null && loggedOutSessions.Contains(sessionId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:7101";
        options.ClientId = "mvc";
        options.ClientSecret = "mvc-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.Scope.Add("permissions");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClaimActions.MapUniqueJsonKey("IsAdmin", "IsAdmin");

        // The ID token is needed to log out of the auth server.
        options.SaveTokens = true;

        // Keep claim names as they are - including "sid", the auth server's session id.
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
