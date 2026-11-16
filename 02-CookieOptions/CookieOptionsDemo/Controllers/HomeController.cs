using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieOptionsDemo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Secret()
    {
        // Besides claims, the cookie stores properties such as when it was issued and when it expires.
        var result = await HttpContext.AuthenticateAsync("MyCookie");
        return View(result.Properties);
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin()
    {
        return View();
    }
}
