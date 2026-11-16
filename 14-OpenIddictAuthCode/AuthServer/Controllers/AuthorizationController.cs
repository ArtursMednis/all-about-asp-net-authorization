using AuthServer.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Controllers;

public class AuthorizationController(UserService userService) : Controller
{
    // Step 1 of the flow: the client app redirects the browser here.
    // OpenIddict has already checked the client id, redirect URI, scopes and PKCE.
    [HttpGet("connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()!;

        // Is the user logged in to the auth server? If not, show our login page
        // and come back to this exact URL afterwards.
        var login = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!login.Succeeded)
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + Request.QueryString },
                CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // The user is logged in. Describe them for the tokens.
        // "sub" (subject) is the user's unique id - the only claim the ID token needs.
        // Everything else the client can fetch from the userinfo endpoint.
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.SetClaim(Claims.Subject, login.Principal.Identity!.Name);
        identity.SetScopes(request.GetScopes());
        identity.SetDestinations(_ => [Destinations.AccessToken, Destinations.IdentityToken]);

        // OpenIddict creates an authorization code and redirects back to the client.
        // The client then exchanges the code for tokens at /connect/token (handled by OpenIddict).
        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // The resource of this tutorial: information about the user, protected by the access token.
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("connect/userinfo")]
    public IActionResult UserInfo()
    {
        var userName = User.GetClaim(Claims.Subject)!;

        // Return only what the granted scopes allow.
        var claims = new Dictionary<string, object> { [Claims.Subject] = userName };

        if (User.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = userService.GetDisplayName(userName);
        }

        if (User.HasScope("permissions"))
        {
            claims["IsAdmin"] = userService.IsAdmin(userName) ? "true" : "false";
        }

        return Ok(claims);
    }

    // Logging out of the client also logs out of the auth server,
    // otherwise the next login would happen silently with the old session.
    [HttpGet("connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // OpenIddict redirects back to the client's post-logout URI.
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
