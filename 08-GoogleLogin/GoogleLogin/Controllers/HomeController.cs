using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GoogleLogin.Controllers;

public class HomeController(UserManager<IdentityUser> userManager) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Secret()
    {
        // Which external accounts are linked to this user?
        var user = (await userManager.GetUserAsync(User))!;
        ViewBag.Logins = await userManager.GetLoginsAsync(user);
        ViewBag.HasPassword = await userManager.HasPasswordAsync(user);
        return View();
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin()
    {
        return View();
    }
}
