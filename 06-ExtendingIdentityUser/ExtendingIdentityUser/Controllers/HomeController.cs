using ExtendingIdentityUser.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExtendingIdentityUser.Controllers;

public class HomeController(UserManager<AppUser> userManager) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Secret()
    {
        // DisplayName is not in the cookie, so load the user from the database.
        // GetUserAsync finds the user by the NameIdentifier claim.
        var user = await userManager.GetUserAsync(User);
        return View(user);
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin()
    {
        return View();
    }
}
