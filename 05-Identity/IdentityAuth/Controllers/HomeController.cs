using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAuth.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Any logged-in user.
    [Authorize]
    public IActionResult Secret()
    {
        return View();
    }

    // Only users that satisfy the "AdminOnly" policy (see Program.cs).
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin()
    {
        return View();
    }
}
