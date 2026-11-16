using Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

// Tutorial 01's AccountController, answering with JSON instead of views.
[ApiController]
[Route("api/account")]
public class AccountController(UserService userService) : ControllerBase
{
    public record LoginRequest(string UserName, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!userService.VerifyPassword(request.UserName, request.Password))
        {
            return Unauthorized(new { error = "Invalid user name or password" });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, request.UserName)
        };

        if (userService.IsAdmin(request.UserName))
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // Same as tutorial 01: the response sets the cookie. The SPA never sees it.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(CurrentUser(new ClaimsPrincipal(identity)));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    // The SPA calls this on startup to find out whether the user is already logged in
    // (the cookie survives page reloads, but JavaScript can't read it).
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(CurrentUser(User));
    }

    private static object CurrentUser(ClaimsPrincipal user) => new
    {
        name = user.Identity!.Name,
        isAdmin = user.HasClaim("IsAdmin", "true"),
    };
}
