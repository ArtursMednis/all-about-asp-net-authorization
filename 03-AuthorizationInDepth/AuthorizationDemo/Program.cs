using AuthorizationDemo.AuthorizationRules;
using AuthorizationDemo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<DocumentService>();

// Authentication is unchanged from tutorial 01.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization(options =>
{
    // 1. Fallback policy: applies to every endpoint WITHOUT an [Authorize] or [AllowAnonymous].
    //    Secure by default - forgetting an attribute no longer makes a page public.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // 2. Roles are claims too ("role" claims). A policy can require one...
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    // 3. ...or a custom requirement, checked by a handler (MinimumAgeHandler).
    options.AddPolicy("AtLeast18", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));

    // 4. Resource-based: checked in code with IAuthorizationService, with the document in hand.
    options.AddPolicy("EditDocument", policy => policy.Requirements.Add(new EditDocumentRequirement()));
});

// Handlers are services. All handlers for a requirement are called.
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DocumentOwnerHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminCanEditAnyDocumentHandler>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
