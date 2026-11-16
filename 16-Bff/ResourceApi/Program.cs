using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// The resource API from tutorial 13. It only accepts access tokens - it has no idea
// that a BFF or a browser exists. The BFF calls it with the user's access token.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7121";
        options.Audience = "resource_api";

        options.MapInboundClaims = false;

        // These tokens are issued for a user, so "name" is the user's name.
        options.TokenValidationParameters.NameClaimType = "name";

        // By default a token is accepted up to 5 minutes after it expired.
        // Turn that off, so the 30-second tokens in this demo really expire.
        options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
    });

builder.Services.AddAuthorization(options =>
{
    // The same policy as tutorial 01 - the claim now arrives in the access token.
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
