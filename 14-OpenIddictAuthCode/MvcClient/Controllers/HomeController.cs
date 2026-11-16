using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MvcClient.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Secret()
    {
        // The tokens we got from the auth server (saved because of SaveTokens = true).
        ViewBag.IdToken = await HttpContext.GetTokenAsync("id_token");
        ViewBag.AccessToken = await HttpContext.GetTokenAsync("access_token");
        return View();
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Admin()
    {
        return View();
    }
}
