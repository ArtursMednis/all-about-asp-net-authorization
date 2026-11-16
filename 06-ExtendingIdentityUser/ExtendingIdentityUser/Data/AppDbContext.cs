using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExtendingIdentityUser.Data;

// The generic argument tells Identity to use AppUser instead of IdentityUser.
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options);
