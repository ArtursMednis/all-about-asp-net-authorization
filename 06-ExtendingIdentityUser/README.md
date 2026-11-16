# 06 - Extending IdentityUser

Add your own properties to ASP.NET Core Identity users - here a `DisplayName`. Covers the three steps (custom user class, registration, using it everywhere) and an important detail: custom properties live in the **database**, not in the login cookie, so you read them with `UserManager`.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 06-ExtendingIdentityUser
dotnet run --project ExtendingIdentityUser
```

Open http://localhost:5004. Log in as `user` / `user` ("Regular User") or `admin` / `admin` ("Administrator").

## What to try

1. Log in and open **Secret**: it greets you with your display name.
2. Look at the claims table on the same page: `DisplayName` is **not** there - it was loaded from the database.

## Key code

### Custom user (`Data/AppUser.cs`)

```csharp
public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
```

Every property becomes a column in the `AspNetUsers` table.

### Registration (`Data/AppDbContext.cs`, `Program.cs`)

Tell Identity to use `AppUser` instead of `IdentityUser`:

```csharp
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options);

builder.Services
    .AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
```

### Using it

Everywhere Identity was used with `IdentityUser`, use `AppUser`:

```csharp
UserManager<AppUser> userManager
SignInManager<AppUser> signInManager

new AppUser
{
    UserName = "admin",
    DisplayName = "Administrator"
};
```

Forgetting one place gives an error like *Unable to resolve service for type `UserManager<IdentityUser>`*.

### Reading it (`Controllers/HomeController.cs`)

```csharp
var user = await userManager.GetUserAsync(User);
user.DisplayName
```

`GetUserAsync` finds the user by the `NameIdentifier` claim in the cookie and loads it from the database.

## Project structure

| File | What it does |
|---|---|
| `Data/AppUser.cs` | The custom user class |
| `Data/AppDbContext.cs` | `IdentityDbContext<AppUser>` |
| `Program.cs` | `AddIdentity<AppUser, IdentityRole>` |
| `Data/SeedData.cs` | Demo users with display names |
| `Controllers/HomeController.cs` | Loads the user to read `DisplayName` |
