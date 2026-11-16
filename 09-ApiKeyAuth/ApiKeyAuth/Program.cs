using ApiKeyAuth.Authentication;
using ApiKeyAuth.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<ApiKeyStore>();

// Our own scheme, registered exactly like the built-in ones.
builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddApiKey(options => options.HeaderName = "X-Api-Key");

// Authorization doesn't care how the user was authenticated - it only sees claims.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
