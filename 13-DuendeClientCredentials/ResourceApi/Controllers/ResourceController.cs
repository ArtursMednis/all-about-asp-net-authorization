using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceApi.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    // Any valid token issued for this API.
    [Authorize]
    [HttpGet("api/secret")]
    public IActionResult Secret()
    {
        return Ok(new
        {
            message = $"Hello, {User.Identity!.Name}",
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
        });
    }

    // Only tokens with the "admin" scope.
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only clients with the admin scope can see this." });
    }
}
