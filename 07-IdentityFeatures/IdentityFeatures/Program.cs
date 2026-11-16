using IdentityFeatures.Data;
using IdentityFeatures.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AuthTutorial"));
builder.Services.AddSingleton<EmailSender>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Relaxed rules so demo passwords like "user" are accepted. Don't do this in production.
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Registration: no login until the email address is confirmed.
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;

        // Lockout: 3 wrong passwords in a row lock the account for 1 minute.
        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    // Token providers create the email confirmation links and check authenticator app codes.
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "true"));
});

var app = builder.Build();

await app.SeedUsersAsync();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
