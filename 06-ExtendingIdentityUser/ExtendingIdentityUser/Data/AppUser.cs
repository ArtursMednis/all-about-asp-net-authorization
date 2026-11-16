using Microsoft.AspNetCore.Identity;

namespace ExtendingIdentityUser.Data;

// Inherit from IdentityUser to add your own columns to the users table.
public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
