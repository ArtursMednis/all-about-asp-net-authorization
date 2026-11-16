using Microsoft.AspNetCore.Identity;

namespace MultipleSchemes.Services;

// A fake user store. Passwords are stored hashed - never store them in plain text.
public class UserService
{
    private readonly PasswordHasher<string> passwordHasher = new();
    private readonly Dictionary<string, (string PasswordHash, bool IsAdmin)> users = new();

    public UserService()
    {
        AddUser("user", "user", isAdmin: false);
        AddUser("admin", "admin", isAdmin: true);
    }

    private void AddUser(string userName, string password, bool isAdmin)
    {
        users[userName] = (passwordHasher.HashPassword(userName, password), isAdmin);
    }

    public bool VerifyPassword(string userName, string password)
    {
        return users.TryGetValue(userName, out var user)
            && passwordHasher.VerifyHashedPassword(userName, user.PasswordHash, password) != PasswordVerificationResult.Failed;
    }

    public bool IsAdmin(string userName)
    {
        return users.TryGetValue(userName, out var user) && user.IsAdmin;
    }
}
