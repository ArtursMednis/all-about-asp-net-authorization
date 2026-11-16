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
        return interaction.IsValidReturnUrl(returnUrl) ? Redirect(returnUrl!) : Content("Logged in to the auth server.");
    }

    // IdentityServer's end session endpoint sends the user here with a logoutId.
    [HttpGet]
    public async Task<IActionResult> Logout(string logoutId)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId, HttpContext.RequestAborted);

        await HttpContext.SignOutAsync();

        // Back to the client app (its PostLogoutRedirectUri).
        return Redirect(context.PostLogoutRedirectUri ?? "/");
    }
}
