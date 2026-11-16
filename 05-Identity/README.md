# 05 - ASP.NET Core Identity

Cookie login with **ASP.NET Core Identity** instead of hand-written code: users, password hashes and claims live in a database (EF Core, in-memory here), `UserManager` manages users, and `SignInManager.PasswordSignInAsync` does the whole login in one call. Authorization (`[Authorize]`, policies) works exactly as with any other cookie login.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 05-Identity
dotnet run --project IdentityAuth
```

Open http://localhost:5003.

| User | Password | |
|---|---|---|
| `user` | `user` | regular user |
| `admin` | `admin` | has the `IsAdmin` claim (stored in the database) |

The database is in memory, so the two users are created again on every start.

## What to try

1. Click **Secret** and log in as `user`. The claims table shows what Identity puts into the cookie: a user id, the name, a security stamp - and, for `admin`, the `IsAdmin` claim from the database.
2. **Admin** gives *Access denied* for `user` and works for `admin`.

## Key code

### Service configuration (`Program.cs`)

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("AuthTutorial"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        // Relaxed rules so demo passwords like "user" work. Don't do this in production.
        options.Password.RequiredLength = 4;
        options.Password.RequireDigit = false;
        // ...
    })
    .AddEntityFrameworkStores<AppDbContext>();

app.UseAuthentication();
app.UseAuthorization();
```

`AddIdentity` also registers cookie authentication, redirecting to `/Account/Login` and `/Account/AccessDenied`.

### User storage (`Data/AppDbContext.cs`, `Data/SeedData.cs`)

```csharp
// IdentityDbContext brings the tables: users, roles, claims, logins, tokens
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext(options);

// UserManager hashes the password for you
await userManager.CreateAsync(user, password);

// Claims stored in the database are copied into the cookie on login
await userManager.AddClaimAsync(user, new Claim("IsAdmin", "true"));
```

`UserManager` is a scoped service - outside of a request, create a scope first (see `SeedData.cs`).

### Signing in and out (`Controllers/AccountController.cs`)

```csharp
// Checks the password, loads the user's claims and writes the cookie - in one call
var result = await signInManager.PasswordSignInAsync(
    userName, password,
    isPersistent: false,
    lockoutOnFailure: false);

await signInManager.SignOutAsync();
```

### Checking authentication and authorization

Nothing Identity-specific:

```csharp
[Authorize]
public IActionResult Secret() { ... }

[Authorize(Policy = "AdminOnly")]   // policy.RequireClaim("IsAdmin", "true")
public IActionResult Admin() { ... }

User.Identity.Name
User.FindFirstValue(ClaimTypes.Name)
```

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | DbContext, `AddIdentity`, the `AdminOnly` policy |
| `Data/AppDbContext.cs` | `IdentityDbContext` |
| `Data/SeedData.cs` | Creates the demo users and the admin's claim |
| `Controllers/AccountController.cs` | Login with `SignInManager` |
| `Controllers/HomeController.cs` | Public, `[Authorize]` and policy-protected pages |
