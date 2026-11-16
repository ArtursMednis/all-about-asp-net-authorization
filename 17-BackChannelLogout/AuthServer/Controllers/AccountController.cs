using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

// The only code IdentityServer needs from us: the login and logout pages.
// Authorize, token and userinfo endpoints are all built in.
public class AccountController(TestUserStore users, IIdentityServerInteractionService interaction) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl)
    {
        if (!users.ValidateCredentials(userName, password))
        {
            ViewBag.Error = "Invalid user name or password";
            return View();
        }

        var user = users.FindByUsername(userName);

        // Logs the user in to IdentityServer (its own cookie). "sub" is the user's unique id.
        await HttpContext.SignInAsync(new IdentityServerUser(user.SubjectId) { DisplayName = user.Username });

        // returnUrl continues the authorize request. IsValidReturnUrl rejects anything else.
        return interaction.IsValidReturnUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Home");
    }

    // Logout started by a client app: IdentityServer's end session endpoint sends the user here with a logoutId.
    [HttpGet]
    public async Task<IActionResult> Logout(string logoutId)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);

        // Ends the session at the auth server. IdentityServer then sends a back-channel
        // logout token to every other client that took part in this session.
        await HttpContext.SignOutAsync();

        // Back to the client app (its PostLogoutRedirectUri).
        return Redirect(context.PostLogoutRedirectUri ?? "/");
    }

    // Logout started on the auth server itself ("Log out of all apps" on the home page).
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Logout")]
    public async Task<IActionResult> LogoutPost()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
