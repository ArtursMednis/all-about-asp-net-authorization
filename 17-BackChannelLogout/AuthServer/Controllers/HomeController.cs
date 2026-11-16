using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers;

// The auth server's own page: shows who is logged in here,
// with a button to log out of every app at once.
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
