using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationDemo.Controllers;

public class HomeController : Controller
{
    // Public - opts out of the fallback policy.
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    // No attribute at all: the fallback policy still requires a logged-in user.
    public IActionResult Secret()
    {
        return View();
    }

    // Roles directly on the attribute. Same as [Authorize(Policy = "AdminOnly")].
    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    // A custom requirement (MinimumAgeHandler).
    [Authorize(Policy = "AtLeast18")]
    public IActionResult Adults()
    {
        return View();
    }
}
