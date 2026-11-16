using Microsoft.AspNetCore.Identity;

namespace TokenAuthApi.Services;

// A fake client store - like UserService in tutorial 01, but for applications instead of people.
// Each client has a hashed secret and the scopes it is allowed to ask for.
public class ClientService
{
    private readonly PasswordHasher<string> secretHasher = new();
    private readonly Dictionary<string, (string SecretHash, string[] AllowedScopes)> clients = new();

    public ClientService()
    {
        AddClient("client", "client-secret", allowedScopes: []);
        AddClient("admin-client", "admin-secret", allowedScopes: ["admin"]);
    }

    private void AddClient(string clientId, string secret, string[] allowedScopes)
    {
        clients[clientId] = (secretHasher.HashPassword(clientId, secret), allowedScopes);
    }

    public bool VerifySecret(string clientId, string secret)
    {
        return clients.TryGetValue(clientId, out var client)
            && secretHasher.VerifyHashedPassword(clientId, client.SecretHash, secret) != PasswordVerificationResult.Failed;
    }

    public string[] GetAllowedScopes(string clientId)
    {
        return clients.TryGetValue(clientId, out var client) ? client.AllowedScopes : [];
    }
}
