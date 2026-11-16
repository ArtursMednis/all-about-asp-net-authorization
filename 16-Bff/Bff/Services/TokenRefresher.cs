using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace Bff.Services;

// Keeps the access token in the session cookie fresh, using the refresh token.
// (For production, the Duende.AccessTokenManagement.OpenIdConnect library does this for you.)
public class TokenRefresher(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    ILogger<TokenRefresher> logger)
{
    public async Task RefreshIfNeededAsync(CookieValidatePrincipalContext context)
    {
        // Saved next to the tokens by SaveTokens = true.
        var expiresAt = context.Properties.GetTokenValue("expires_at");
        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (expiresAt == null || refreshToken == null)
        {
            return;
        }

        // Still valid for a while? Nothing to do. (A few seconds early, so it can't expire on the way to the API.)
        if (DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture) > DateTimeOffset.UtcNow.AddSeconds(5))
        {
            return;
        }

        // Ask the auth server's token endpoint for a new access token.
        var oidc = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var configuration = await oidc.ConfigurationManager!.GetConfigurationAsync(context.HttpContext.RequestAborted);

        var response = await httpClientFactory.CreateClient().PostAsync(configuration.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = oidc.ClientId!,
            ["client_secret"] = oidc.ClientSecret!,
        }));

        if (!response.IsSuccessStatusCode)
        {
            // The refresh token expired or was revoked - the user has to log in again.
            logger.LogWarning("Token refresh failed ({Status}), ending the session", response.StatusCode);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var tokens = await response.Content.ReadFromJsonAsync<JsonElement>();

        context.Properties.UpdateTokenValue("access_token", tokens.GetProperty("access_token").GetString()!);
        context.Properties.UpdateTokenValue("expires_at",
            DateTimeOffset.UtcNow.AddSeconds(tokens.GetProperty("expires_in").GetInt32()).ToString("o", CultureInfo.InvariantCulture));

        // With refresh token rotation the auth server sends a new refresh token too.
        if (tokens.TryGetProperty("refresh_token", out var newRefreshToken))
        {
            context.Properties.UpdateTokenValue("refresh_token", newRefreshToken.GetString()!);
        }

        // Write the session cookie again, now with the new tokens.
        context.ShouldRenew = true;

        logger.LogInformation("Refreshed the access token for {User}", context.Principal?.Identity?.Name);
    }
}
