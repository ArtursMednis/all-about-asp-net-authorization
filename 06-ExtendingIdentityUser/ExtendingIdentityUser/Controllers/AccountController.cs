using ExtendingIdentityUser.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExtendingIdentityUser.Controllers;

public class AccountController(SignInManager<AppUser> signInManager) : Controller
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
        // Checks the password, loads the user's claims and signs in with a cookie - all in one call.
        var result = await signInManager.PasswordSignInAsync(userName, password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ViewBag.Error = "Invalid user name or password";
            return View();
        }

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
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
}
