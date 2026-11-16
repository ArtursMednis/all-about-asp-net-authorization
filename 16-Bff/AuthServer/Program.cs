using AuthServer;

var builder = WebApplication.CreateBuilder(args);

// For our login page.
builder.Services.AddControllersWithViews();

builder.Services.AddIdentityServer(options =>
    {
        // Automatic key management stores keys in a "keys" folder. We use a temporary key instead.
        options.KeyManagement.Enabled = false;

        // Where IdentityServer sends the user to log in and out (these are the defaults).
        options.UserInteraction.LoginUrl = "/Account/Login";
        options.UserInteraction.LogoutUrl = "/Account/Logout";
    })
    .AddDeveloperSigningCredential(persistKey: false)
    .AddInMemoryIdentityResources(Config.IdentityResources)
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    // The users, and a profile service that returns their claims to userinfo.
    .AddTestUsers(Config.Users);

var app = builder.Build();

// The OAuth 2.0 / OpenID Connect endpoints: discovery, authorize, token, userinfo, end session.
// IdentityServer also sets up its own login cookie, so no AddAuthentication is needed.
app.UseIdentityServer();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
