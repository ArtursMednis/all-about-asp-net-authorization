using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiKeyAuth.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    // Any valid API key.
    [Authorize]
    [HttpGet("api/secret")]
    public IActionResult Secret()
    {
        return Ok(new
        {
            message = $"Hello, {User.Identity!.Name}",
            authenticationType = User.Identity.AuthenticationType, // "ApiKey" - our scheme
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
        });
    }

    // Only keys of admin clients.
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only admin clients can see this." });
    }
}
