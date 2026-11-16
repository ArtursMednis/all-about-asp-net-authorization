using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityFeatures.Controllers;

// Turn on two-factor authentication with an authenticator app
// (Microsoft Authenticator, Google Authenticator, 1Password, ...).
[Authorize]
public class TwoFactorController(UserManager<IdentityUser> userManager) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = (await userManager.GetUserAsync(User))!;

        // The shared secret between our app and the user's authenticator app.
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (key == null)
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        ViewBag.Enabled = await userManager.GetTwoFactorEnabledAsync(user);
        ViewBag.Key = key;

        // Authenticator apps read this URI from a QR code - or you type in the key by hand.
        ViewBag.Uri = $"otpauth://totp/AuthTutorials:{Uri.EscapeDataString(user.UserName!)}?secret={key}&issuer=AuthTutorials&digits=6";

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(string code)
    {
        var user = (await userManager.GetUserAsync(User))!;

        // Only turn it on once the user has proven their app produces the right codes.
        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user, userManager.Options.Tokens.AuthenticatorTokenProvider, code.Replace(" ", ""));

        if (valid)
        {
            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
        else
        {
            TempData["Error"] = "That code is not valid. Check the time on your phone and try again.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable()
    {
        var user = (await userManager.GetUserAsync(User))!;
        await userManager.SetTwoFactorEnabledAsync(user, false);

        // A new key, so the old app entry stops working.
        await userManager.ResetAuthenticatorKeyAsync(user);
        return RedirectToAction("Index");
    }
}
