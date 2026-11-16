using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MultipleSchemes.Services;
using System.Security.Claims;

namespace MultipleSchemes.Controllers;

[ApiController]
[Route("api")]
public class ApiController(UserService userService, JwtSettings jwt) : ControllerBase
{
    public record TokenRequest(string UserName, string Password);

    // Apps log in here and get a token (a simple version of tutorial 10's token endpoint).
    [HttpPost("token")]
    public IActionResult Token(TokenRequest request)
    {
        if (!userService.VerifyPassword(request.UserName, request.Password))
        {
            return Unauthorized();
        }

        var claims = new List<Claim> { new("name", request.UserName) };
        if (userService.IsAdmin(request.UserName))
        {
            claims.Add(new Claim("IsAdmin", "true"));
        }

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = DateTime.UtcNow.AddMinutes(jwt.DurationInMinutes),
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
        });

        return Ok(new { access_token = token });
    }

    // Default scheme ("CookieOrBearer"): works from the browser (cookie) AND from apps (token).
    [Authorize]
    [HttpGet("secret")]
    public IActionResult Secret()
    {
        return Ok(new
        {
            message = $"Hello, {User.Identity!.Name}",
            // Which scheme let you in? "Cookies" or "Bearer".
            authenticatedBy = User.Identity.AuthenticationType,
        });
    }

    // Only this scheme counts here: the browser's cookie is ignored, you need a token.
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("bearer-only")]
    public IActionResult BearerOnly()
    {
        return Ok(new { message = $"Hello, {User.Identity!.Name}. You used a bearer token." });
    }

    // A policy - the same for cookie and token users.
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Only admins can see this - however they logged in." });
    }
}
