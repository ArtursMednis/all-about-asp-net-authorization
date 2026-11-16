using System.Security.Cryptography;
using System.Text;

namespace ApiKeyAuth.Services;

public record ApiClient(string Name, bool IsAdmin);

// Our API keys. Like passwords, they are stored hashed - a leaked database doesn't leak the keys.
// (A plain SHA-256 is enough here: unlike passwords, API keys are long and random.)
public class ApiKeyStore
{
    private readonly Dictionary<string, ApiClient> clientsByKeyHash = new()
    {
        [Hash("key-for-client")] = new ApiClient("client", IsAdmin: false),
        [Hash("key-for-admin-client")] = new ApiClient("admin-client", IsAdmin: true),
    };

    public ApiClient? Find(string apiKey)
    {
        return clientsByKeyHash.GetValueOrDefault(Hash(apiKey));
    }

    private static string Hash(string apiKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
}
