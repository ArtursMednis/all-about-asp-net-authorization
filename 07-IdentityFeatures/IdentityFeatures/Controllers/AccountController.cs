using IdentityFeatures.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityFeatures.Controllers;

public class AccountController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    EmailSender emailSender) : Controller
{
    // ------------------------------------------------------------ registration

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password)
    {
        var user = new IdentityUser { UserName = email, Email = email };

        // Validates the password rules and that the email isn't taken, then stores the user.
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            ViewBag.Errors = result.Errors.Select(e => e.Description);
            return View();
        }

        // A one-time token proves the user can read emails sent to this address.
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

        emailSender.Send(email, "Confirm your email", $"Click to confirm: {link}");
        return View("CheckEmail");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId);
        var result = user == null ? IdentityResult.Failed() : await userManager.ConfirmEmailAsync(user, token);

        ViewBag.Confirmed = result.Succeeded;
        return View();
    }

    // ------------------------------------------------------------ login (with lockout and 2FA)

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl)
    {
        // lockoutOnFailure: true counts wrong passwords and locks the account after too many.
        var result = await signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        // The password was right, but the user has two-factor authentication turned on.
        if (result.RequiresTwoFactor)
        {
            return RedirectToAction("LoginWith2fa", new { returnUrl });
        }

        ViewBag.Error = result.IsLockedOut ? "Too many wrong passwords. The account is locked for a minute."
            : result.IsNotAllowed ? "Please confirm your email address first."
            : "Invalid user name or password";
        return View();
    }

    [HttpGet]
    public IActionResult LoginWith2fa()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(string code, string? returnUrl)
    {
        // Checks the 6-digit code from the authenticator app. SignInManager remembers
        // (in a temporary cookie) which user already passed the password step.
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(code.Replace(" ", ""), isPersistent: false, rememberClient: false);

        if (result.Succeeded)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        ViewBag.Error = result.IsLockedOut ? "Too many wrong codes. The account is locked for a minute." : "Invalid code";
        return View();
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

    private IActionResult LocalRedirectOrHome(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}
