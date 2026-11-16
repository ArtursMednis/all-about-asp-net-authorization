using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MultipleSchemes.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwt);

// Two ways in: a cookie for people using the web pages (tutorial 01),
// a bearer token for apps calling the API (tutorial 10).
builder.Services.AddAuthentication("CookieOrBearer")

    // A policy scheme doesn't authenticate anything itself.
    // For every request it picks one of the real schemes and forwards to it.
    .AddPolicyScheme("CookieOrBearer", "Cookie or bearer token", options =>
    {
        options.ForwardDefaultSelector = context =>
            context.Request.Headers.Authorization.ToString().StartsWith("Bearer ")
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
    })

    .AddCookie()

    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.SigningKey,
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            NameClaimType = "name",

            // Shown as User.Identity.AuthenticationType (the default is "AuthenticationTypes.Federation").
            AuthenticationType = JwtBearerDefaults.AuthenticationScheme,
        };
    });

// Policies work with any scheme - they only look at claims.
// Both the cookie and the token carry an "IsAdmin" claim for the admin.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
