using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Controllers;

public class TokenController(IOpenIddictScopeManager scopeManager) : Controller
{
    // By the time we get here OpenIddict has already checked the request:
    // the client secret, the grant type and that the client may ask for these scopes.
    // Our job is only to decide which claims go into the token.
    [HttpPost("connect/token")]
    public async Task<IActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;

        if (!request.IsClientCredentialsGrantType())
        {
            return BadRequest();
        }

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        // In the client credentials flow there is no user - the client itself is the subject.
        identity.SetClaim(Claims.Subject, request.ClientId);

        // The granted scopes, and the APIs they belong to (they become the "aud" claim).
        identity.SetScopes(request.GetScopes());
        identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        // Put every claim into the access token.
        identity.SetDestinations(_ => [Destinations.AccessToken]);

        // OpenIddict turns this into the token response.
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
