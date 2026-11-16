using CookieOptionsDemo.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CookieOptionsDemo.Controllers;

public class AccountController(UserService userService) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, bool rememberMe, string? returnUrl)
    {
        if (!userService.VerifyPassword(userName, password))
        {
            ViewBag.Error = "Invalid user name or password";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName)
        };

        if (userService.IsAdmin(userName))
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var identity = new ClaimsIdentity(claims, "MyCookie");

        // Per-login settings that override the options from Program.cs.
        var properties = new AuthenticationProperties
        {
            // true:  the cookie gets an Expires date and survives closing the browser.
            // false: a session cookie, deleted when the browser closes.
            // In both cases the login itself still expires after ExpireTimeSpan.
            IsPersistent = rememberMe
        };

        await HttpContext.SignInAsync("MyCookie", new ClaimsPrincipal(identity), properties);

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookie");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
