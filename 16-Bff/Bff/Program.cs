using Bff.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TokenRefresher>();

// ---------------------------------------------------------------- login
// Exactly the MVC client from tutorial 15: a cookie for the session, OpenID Connect to log in.
// The tokens are kept inside the encrypted cookie (SaveTokens) - the SPA never sees them.
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "Bff";

        // Lax, not Strict: the browser comes back from the auth server (another site in production)
        // and must already send the cookie on that navigation. CSRF is handled by the X-CSRF header below.
        options.Cookie.SameSite = SameSiteMode.Lax;

        // On every request: refresh the access token if it's about to expire.
        options.Events.OnValidatePrincipal = context =>
            context.HttpContext.RequestServices.GetRequiredService<TokenRefresher>().RefreshIfNeededAsync(context);
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = "https://localhost:7121";
        options.ClientId = "bff";
        options.ClientSecret = "bff-secret";
        options.ResponseType = OpenIdConnectResponseType.Code;

        options.Scope.Add("permissions");
        options.Scope.Add("api");            // call the resource API
        options.Scope.Add("offline_access"); // and get a refresh token

        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClaimActions.MapUniqueJsonKey("IsAdmin", "IsAdmin");

        options.SaveTokens = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
    });

// ---------------------------------------------------------------- reverse proxy (YARP)
// /api/** is forwarded to the resource API, with the user's access token attached.
var routes = new List<RouteConfig>
{
    new() { RouteId = "api", ClusterId = "api", Match = new() { Path = "/api/{**rest}" } },
};
var clusters = new List<ClusterConfig>
{
    new() { ClusterId = "api", Destinations = new Dictionary<string, DestinationConfig> { ["api"] = new() { Address = "https://localhost:7123" } } },
};

// In development everything else goes to the Vite dev server, so the SPA and the BFF share one origin.
// (In production the built SPA is served from wwwroot instead - see below.)
if (builder.Environment.IsDevelopment())
{
    routes.Add(new() { RouteId = "spa", ClusterId = "spa", Match = new() { Path = "{**catch-all}" }, Order = int.MaxValue });
    clusters.Add(new() { ClusterId = "spa", Destinations = new Dictionary<string, DestinationConfig> { ["spa"] = new() { Address = "http://localhost:5122" } } });
}

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters)
    .AddTransforms(context =>
    {
        if (context.Route.RouteId != "api")
        {
            return;
        }

        // The heart of the BFF: the browser sends a cookie, the API receives a token.
        context.AddRequestTransform(async transform =>
        {
            var accessToken = await transform.HttpContext.GetTokenAsync("access_token");
            if (accessToken != null)
            {
                transform.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        });
    });

var app = builder.Build();

// ---------------------------------------------------------------- CSRF protection
// The SPA calls these with fetch and always adds "X-CSRF: 1". A browser only sends a custom header
// from another site after a CORS preflight - which this app never allows. So a request without
// the header did not come from our SPA.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if ((path.StartsWithSegments("/api") || path.StartsWithSegments("/bff/user")) && context.Request.Headers["X-CSRF"] != "1")
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles(); // the built SPA (npm run build writes it to wwwroot)
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapReverseProxy();

if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
