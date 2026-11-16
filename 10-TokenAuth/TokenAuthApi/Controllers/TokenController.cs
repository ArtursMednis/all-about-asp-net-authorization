using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using TokenAuthApi.Services;

namespace TokenAuthApi.Controllers;

// The token endpoint. Works like the OAuth 2.0 "client credentials" flow:
// an application sends its id and secret and gets back an access token.
[ApiController]
public class TokenController(ClientService clientService, JwtSettings jwt) : ControllerBase
{
    [HttpPost("connect/token")]
    public IActionResult Token(
        [FromForm(Name = "grant_type")] string grantType,
        [FromForm(Name = "client_id")] string clientId,
        [FromForm(Name = "client_secret")] string clientSecret,
        [FromForm(Name = "scope")] string? scope)
    {
        if (grantType != "client_credentials")
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        if (!clientService.VerifySecret(clientId, clientSecret))
        {
            return Unauthorized(new { error = "invalid_client" });
        }

        // The client may only get scopes it is allowed to have.
        var requestedScopes = scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (requestedScopes.Except(clientService.GetAllowedScopes(clientId)).Any())
        {
            return BadRequest(new { error = "invalid_scope" });
        }

        // Claims work exactly like in the cookie tutorials - they just end up in a token.
        var claims = new List<Claim>
        {
            new("sub", clientId)
        };

        if (requestedScopes.Length > 0)
        {
            claims.Add(new Claim("scope", string.Join(' ', requestedScopes)));
        }

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = DateTime.UtcNow.AddMinutes(jwt.DurationInMinutes),

            // The signature proves the token was created by us and hasn't been changed.
            SigningCredentials = new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256),
        });

        // Standard OAuth 2.0 token response.
        return Ok(new
        {
            access_token = token,
            token_type = "Bearer",
            expires_in = jwt.DurationInMinutes * 60,
            scope = string.Join(' ', requestedScopes),
        });
    }
}
