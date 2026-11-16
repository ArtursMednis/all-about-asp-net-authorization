using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ExtendingIdentityUser.Data;

public static class SeedData
{
    public static async Task SeedUsersAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await CreateUserAsync(userManager, "user", "user", "Regular User", isAdmin: false);
        await CreateUserAsync(userManager, "admin", "admin", "Administrator", isAdmin: true);
    }

    private static async Task CreateUserAsync(UserManager<AppUser> userManager, string userName, string password, string displayName, bool isAdmin)
    {
        var user = new AppUser { UserName = userName, DisplayName = displayName };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception("Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (isAdmin)
        {
            await userManager.AddClaimAsync(user, new Claim("IsAdmin", "true"));
        }
    }
}
