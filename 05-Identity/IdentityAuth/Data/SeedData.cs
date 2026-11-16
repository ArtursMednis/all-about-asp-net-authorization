using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityAuth.Data;

public static class SeedData
{
    public static async Task SeedUsersAsync(this WebApplication app)
    {
        // UserManager is a scoped service, so we need a scope outside of a request.
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        await CreateUserAsync(userManager, "user", "user", isAdmin: false);
        await CreateUserAsync(userManager, "admin", "admin", isAdmin: true);
    }

    private static async Task CreateUserAsync(UserManager<IdentityUser> userManager, string userName, string password, bool isAdmin)
    {
        var user = new IdentityUser { UserName = userName };

        // UserManager hashes the password for us.
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception("Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // Claims stored in the database are copied into the cookie on login.
        if (isAdmin)
        {
            await userManager.AddClaimAsync(user, new Claim("IsAdmin", "true"));
        }
    }
}
