using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    // Any logged-in user. Reading claims works exactly like in tutorial 01.
    [Authorize]
    [HttpGet("api/secret")]
    public IActionResult Secret()
    {
        return Ok(new
        {
            message = $"Hello, {User.FindFirstValue(ClaimTypes.Name)}. This is a protected resource.",
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
        });
    }

    // Only users with the IsAdmin claim. Hiding a button in the SPA is not security - this is.
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only users with the IsAdmin claim can see this." });
    }
}
