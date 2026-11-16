using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Same JwtBearer as tutorial 10, but this API doesn't create tokens and doesn't know any keys.
// It trusts the auth server and downloads everything it needs from its discovery document:
//   https://localhost:7071/.well-known/openid-configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The auth server. Sets the valid issuer and fetches its signing keys.
        options.Authority = "https://localhost:7071";

        // Only accept tokens issued for this API (the "aud" claim).
        options.Audience = "resource_api";

        // Signature, issuer, audience and lifetime are validated by default.
        // Keep claim names as they are in the token.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "client_id";
    });

builder.Services.AddAuthorization(options =>
{
    // Tokens can carry scopes as one space-separated "scope" claim ("api admin")
    // or as several "scope" claims. This check works for both.
    options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
        context.User.FindAll("scope").Any(c => c.Value.Split(' ').Contains("admin"))));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
