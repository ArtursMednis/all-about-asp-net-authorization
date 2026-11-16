using CookieOptionsDemo.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();

// A scheme is just a name. Tutorial 01 used the built-in "Cookies" name;
// here we pick our own and must use it consistently (see AccountController).
builder.Services.AddAuthentication("MyCookie")
    .AddCookie("MyCookie", options =>
    {
        // --- The cookie itself ---

        // Name in the browser. Default: ".AspNetCore." + scheme name.
        options.Cookie.Name = "MyAuthCookie";

        // JavaScript can't read the cookie. Default: true.
        options.Cookie.HttpOnly = true;

        // HTTPS only? Default: SameAsRequest. Use Always in production.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Whether the browser sends the cookie on cross-site requests. Default: Lax.
        options.Cookie.SameSite = SameSiteMode.Lax;

        // --- Lifetime of the login ---

        // How long the login is valid. Default: 14 days. Short here so you can watch it expire.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(1);

        // Issue a fresh cookie when more than half of ExpireTimeSpan has passed. Default: true.
        options.SlidingExpiration = true;

        // --- Redirects (these are the defaults) ---

        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ReturnUrlParameter = "ReturnUrl";
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
