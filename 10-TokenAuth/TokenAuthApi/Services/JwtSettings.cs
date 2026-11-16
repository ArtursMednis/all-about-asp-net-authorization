using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TokenAuthApi.Services;

// The "Jwt" section of appsettings.json.
// Used both to create tokens (TokenController) and to validate them (Program.cs).
public class JwtSettings
{
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string Key { get; init; }
    public int DurationInMinutes { get; init; }

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));
}
