using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Bff.Controllers;

// The few endpoints the SPA needs to log in and out.
public class BffController : ControllerBase
{
    // GET /bff/login?returnUrl=/secret
    // A normal browser navigation (not fetch): off to the auth server and back to returnUrl.
    [HttpGet("bff/login")]
    public IActionResult Login(string? returnUrl)
    {
        return Challenge(new AuthenticationProperties { RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : "/" });
    }

    // GET /bff/user - who is logged in? Called by the SPA with fetch.
    // 401 instead of a redirect: the SPA decides what to show.
    [HttpGet("bff/user")]
    public IActionResult CurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            name = User.Identity.Name,
            isAdmin = User.HasClaim("IsAdmin", "true"),
            logoutUrl = $"/bff/logout?sid={Uri.EscapeDataString(User.FindFirst("sid")!.Value)}",
        });
    }

    // GET /bff/logout?sid=... - a browser navigation, so it can't carry the X-CSRF header.
    // Instead it must contain the session id, which another site can't know:
    // nobody can log the user out with a simple link.
    [HttpGet("bff/logout")]
    public IActionResult Logout(string? sid)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Redirect("/");
        }

        if (sid != User.FindFirst("sid")?.Value)
        {
            return BadRequest();
        }

        // Sign out of the BFF session and the auth server, like tutorial 15.
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
