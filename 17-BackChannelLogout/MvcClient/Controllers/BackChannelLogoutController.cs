using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MvcClient.Services;
using System.Security.Claims;
using System.Text.Json;

namespace MvcClient.Controllers;

// Called by the auth server, not by the browser:
//   POST /account/backchannel-logout
//   logout_token=<JWT>
public class BackChannelLogoutController(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    LoggedOutSessions loggedOutSessions,
    ILogger<BackChannelLogoutController> logger) : Controller
{
    private const string BackChannelLogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    [HttpPost("account/backchannel-logout")]
    [IgnoreAntiforgeryToken] // server-to-server call, there is no form
    public async Task<IActionResult> Logout([FromForm(Name = "logout_token")] string logoutToken)
    {
        // Anyone can POST to this URL, so the token must be validated like any other token.
        // The issuer and signing keys come from the auth server's discovery document,
        // which the OpenIdConnect handler has already downloaded.
        var oidc = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var configuration = await oidc.ConfigurationManager!.GetConfigurationAsync(HttpContext.RequestAborted);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(logoutToken, new TokenValidationParameters
        {
            ValidIssuer = configuration.Issuer,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAudience = oidc.ClientId, // issued for this app
        });

        if (!result.IsValid || !IsLogoutToken(result.ClaimsIdentity))
        {
            logger.LogWarning("Invalid logout token: {Error}", result.Exception?.Message ?? "not a logout token");
            return BadRequest();
        }

        // The session id tells us exactly which login to end.
        var sessionId = result.ClaimsIdentity.FindFirst("sid")!.Value;
        loggedOutSessions.Add(sessionId);

        logger.LogInformation("Back-channel logout for user {Subject}, session {SessionId}",
            result.ClaimsIdentity.FindFirst("sub")?.Value, sessionId);

        Response.Headers.CacheControl = "no-store";
        return Ok();
    }

    // Rules from the OpenID Connect Back-Channel Logout spec.
    private static bool IsLogoutToken(ClaimsIdentity token)
    {
        // Must say "this is a logout", so an ID token can't be replayed here.
        var events = token.FindFirst("events")?.Value;
        if (events == null || !JsonDocument.Parse(events).RootElement.TryGetProperty(BackChannelLogoutEvent, out _))
        {
            return false;
        }

        // Must never contain a nonce (ID tokens do).
        // We also require "sid" - the client is registered with BackChannelLogoutSessionRequired.
        return token.FindFirst("nonce") == null && token.FindFirst("sid") != null;
    }
}
