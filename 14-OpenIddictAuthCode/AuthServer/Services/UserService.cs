using Microsoft.AspNetCore.Identity;

namespace AuthServer.Services;

// The same fake user store as tutorial 01, plus a display name.
// The users now live on the auth server - the client app never sees their passwords.
public class UserService
{
    private readonly PasswordHasher<string> passwordHasher = new();
    private readonly Dictionary<string, (string PasswordHash, string DisplayName, bool IsAdmin)> users = new();

    public UserService()
    {
        AddUser("user", "user", "Regular User", isAdmin: false);
        AddUser("admin", "admin", "Administrator", isAdmin: true);
    }

    private void AddUser(string userName, string password, string displayName, bool isAdmin)
    {
        users[userName] = (passwordHasher.HashPassword(userName, password), displayName, isAdmin);
    }

    public bool VerifyPassword(string userName, string password)
    {
        return users.TryGetValue(userName, out var user)
            && passwordHasher.VerifyHashedPassword(userName, user.PasswordHash, password) != PasswordVerificationResult.Failed;
    }

    public string GetDisplayName(string userName)
    {
        return users[userName].DisplayName;
    }

    public bool IsAdmin(string userName)
    {
        return users.TryGetValue(userName, out var user) && user.IsAdmin;
    }
}
