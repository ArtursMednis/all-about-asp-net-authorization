using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GoogleLogin.Controllers;

public class AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Login()
    {
        return await LoginView();
    }

    // Local login, exactly as in tutorial 05.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl)
    {
        var result = await signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        return await LoginView("Invalid user name or password");
    }

    // Step 1: "Log in with Google" button. Off to Google...
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl)
    {
        // ...and back to ExternalLoginCallback afterwards.
        var callbackUrl = Url.Action("ExternalLoginCallback", new { returnUrl });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
        return Challenge(properties, provider);
    }

    // Step 2: back from Google. Google's answer is in Identity's external cookie.
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return await LoginView("Login with Google failed.");
        }

        // Is this Google account already linked to one of our users? Then log them in.
        var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        // First login with this Google account: create a local user for it.
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email == null)
        {
            return await LoginView("Google did not share an email address.");
        }

        if (await userManager.FindByEmailAsync(email) != null)
        {
            // Don't silently attach Google to an existing account - that's for a logged-in user to decide.
            return await LoginView("An account with this email already exists. Log in with your password.");
        }

        // Google has verified the address, so we can trust it.
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user); // no password - this user logs in with Google only

        // Remember the link: "Google user <ProviderKey> is our user <user.Id>" (table AspNetUserLogins).
        await userManager.AddLoginAsync(user, info);

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirectOrHome(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task<IActionResult> LoginView(string? error = null)
    {
        // Every registered external provider gets a button (here: Google, if configured).
        ViewBag.ExternalProviders = await signInManager.GetExternalAuthenticationSchemesAsync();
        ViewBag.Error = error;
        return View("Login");
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}
