using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ResourceApi.Controllers;

[ApiController]
public class ResourceController : ControllerBase
{
    // Any valid access token for this API.
    [Authorize]
    [HttpGet("api/secret")]
    public IActionResult Secret()
    {
        // "exp" lets the SPA show when the access token expires - and that the BFF refreshed it.
        var expires = DateTimeOffset.FromUnixTimeSeconds(long.Parse(User.FindFirst("exp")!.Value));

        return Ok(new
        {
            message = $"Hello, {User.Identity!.Name}. This came from the resource API.",
            accessTokenExpires = expires,
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
        });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only users with the IsAdmin claim can see this." });
    }
}
