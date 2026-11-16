using Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<UserService>();

// The same cookie authentication as tutorial 01.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SpaAuth";

        // JavaScript can't read the cookie - an XSS bug can't steal the login.
        options.Cookie.HttpOnly = true;

        // The browser never sends the cookie with requests started by other sites (CSRF protection).
        options.Cookie.SameSite = SameSiteMode.Strict;

        // No redirects to a login page: for [ApiController] endpoints ASP.NET Core
        // answers 401 (not logged in) and 403 (not allowed) - the SPA decides what to show.
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
