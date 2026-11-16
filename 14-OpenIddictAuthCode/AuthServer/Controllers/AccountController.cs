using AuthServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Controllers;

// The auth server's login page - the same code as tutorial 01.
public class AccountController(UserService userService) : Controller
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
        if (!userService.VerifyPassword(userName, password))
        {
            ViewBag.Error = "Invalid user name or password";
            return View();
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // returnUrl is /connect/authorize?... - the flow continues there.
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : Content("Logged in to the auth server.");
    }
}
