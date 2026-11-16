using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TokenAuthApi.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    // Any valid token.
    [Authorize]
    [HttpGet("api/secret")]
    public IActionResult Secret()
    {
        // User is rebuilt from the token on every request - just like from the cookie.
        return Ok(new
        {
            message = $"Hello, {User.Identity!.Name}",
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
        });
    }

    // Only tokens with the "admin" scope (see the "AdminOnly" policy in Program.cs).
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only clients with the admin scope can see this." });
    }
}
