using Microsoft.EntityFrameworkCore;

namespace AuthServer.Data;

// An empty DbContext - UseOpenIddict() in Program.cs adds OpenIddict's entities to it.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
