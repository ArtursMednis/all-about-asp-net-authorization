using ApiKeyAuth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ApiKeyAuth.Authentication;

// Settings for our scheme - like CookieAuthenticationOptions for cookies.
public class ApiKeyOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = "X-Api-Key";
}

// A complete authentication handler. AddCookie and AddJwtBearer register classes just like this one.
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyStore apiKeyStore)
    : AuthenticationHandler<ApiKeyOptions>(options, logger, encoder)
{
    // "Who is calling?" - called by UseAuthentication on every request.
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No key at all: not an error, just anonymous. [Authorize] decides whether that's OK.
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKey))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var client = apiKeyStore.Find(apiKey.ToString());
        if (client == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        // From here on it's the same as every other tutorial: claims -> identity -> principal.
        var claims = new List<Claim> { new(ClaimTypes.Name, client.Name) };
        if (client.IsAdmin)
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    // "Please authenticate" - called when [Authorize] fails and nobody is logged in.
    // Cookies redirect to a login page here; we answer 401 and say how to authenticate.
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var result = await HandleAuthenticateOnceSafeAsync();

        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = result.Failure == null
            ? $"ApiKey header=\"{Options.HeaderName}\""
            : $"ApiKey header=\"{Options.HeaderName}\", error=\"{result.Failure.Message}\"";
    }

    // HandleForbiddenAsync ("logged in, but not allowed") keeps its default: 403.
}

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

// So Program.cs can say .AddApiKey(), like .AddCookie().
public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder, Action<ApiKeyOptions>? configure = null)
    {
        return builder.AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, configure);
    }
}
