using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TokenAuthApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ClientService>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwt);

// Instead of a cookie, every request carries a token in the header:
//   Authorization: Bearer <token>
// Unauthenticated requests get 401 (no redirect - an API has no login page).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names as they are in the token ("sub", "scope")
        // instead of mapping them to long ClaimTypes URIs.
        options.MapInboundClaims = false;

        // Everything a token must pass before we trust it.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Was it signed with our key? Without this, anyone could write their own token.
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.SigningKey,

            // Was it created by us?
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            // Was it created for this API?
            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            // Is it still valid? Default ClockSkew allows 5 minutes extra; we allow none.
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            // Which claim User.Identity.Name reads.
            NameClaimType = "sub",
        };
    });

builder.Services.AddAuthorization(options =>
{
    // The "scope" claim holds space-separated values, e.g. "admin other".
    options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
        context.User.FindFirst("scope")?.Value.Split(' ').Contains("admin") == true));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
