using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace MvcClient.Controllers;

// No login form and no passwords in this app - the auth server handles all of that.
public class AccountController : Controller
{
    // Challenge = "log me in". The default challenge scheme (OpenIdConnect)
    // redirects to the auth server. Opening a page with [Authorize] does the same.
    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Sign out of both: our cookie, and the auth server's session.
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
