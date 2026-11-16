using Microsoft.AspNetCore.Identity;

namespace AuthorizationDemo.Services;

public record UserInfo(string UserName, string[] Roles, DateOnly DateOfBirth);

// The fake user store from tutorial 01, now with roles and a date of birth.
public class UserService
{
    private readonly PasswordHasher<string> passwordHasher = new();
    private readonly Dictionary<string, (string PasswordHash, UserInfo Info)> users = new();

    public UserService()
    {
        AddUser("user", "user", roles: ["Employee"], dateOfBirth: new DateOnly(2012, 5, 1));
        AddUser("admin", "admin", roles: ["Employee", "Admin"], dateOfBirth: new DateOnly(1985, 3, 15));
    }

    private void AddUser(string userName, string password, string[] roles, DateOnly dateOfBirth)
    {
        users[userName] = (passwordHasher.HashPassword(userName, password), new UserInfo(userName, roles, dateOfBirth));
    }

    // Returns the user if the password is right, otherwise null.
    public UserInfo? VerifyPassword(string userName, string password)
    {
        return users.TryGetValue(userName, out var user)
            && passwordHasher.VerifyHashedPassword(userName, user.PasswordHash, password) != PasswordVerificationResult.Failed
            ? user.Info
            : null;
    }
}
