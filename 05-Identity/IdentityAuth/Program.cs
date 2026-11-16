using IdentityAuth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Identity stores users in a database through EF Core. In-memory, so it's empty on every start.
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AuthTutorial"));

// Registers UserManager, SignInManager and the cookie authentication from tutorial 01.
// The cookie redirects to /Account/Login and /Account/AccessDenied by default.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Relaxed rules so demo passwords like "user" are accepted. Don't do this in production.
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();

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
