using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityFeatures.Data;

// IdentityDbContext already contains the tables for users, roles and claims.
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext(options);
