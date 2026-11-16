# 01 - Cookie authentication

The simplest possible login in ASP.NET Core MVC: no ASP.NET Core Identity, no database, no external providers. Users live in memory, a login form checks the password, and the user is remembered with an encrypted cookie that holds their **claims**. Pages are protected with `[Authorize]`, and an admin page with a **policy**.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 01-CookieAuth
dotnet run --project CookieAuth
```

Open http://localhost:5001 (or open `CookieAuth.slnx` in Visual Studio / Rider and press F5).

| User | Password | |
|---|---|---|
| `user` | `user` | regular user |
| `admin` | `admin` | has the `IsAdmin` claim |

## What to try

1. Click **Secret** while logged out: you are redirected to `/Account/Login?ReturnUrl=/Home/Secret`.
2. Log in as `user`: you land back on **Secret**, which lists your claims.
3. Click **Admin**: *Access denied* - you are logged in, but don't have the `IsAdmin` claim.
4. Log out, log in as `admin`: **Admin** works.

## Key code

### Authentication - service configuration (`Program.cs`)

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

app.UseAuthentication();
```

`CookieAuthenticationDefaults.AuthenticationScheme` is just the string `"Cookies"`. With the defaults, anonymous users are redirected to `/Account/Login` and forbidden users to `/Account/AccessDenied`.

### Signing in and out (`Controllers/AccountController.cs`)

```csharp
var claims = new List<Claim>
{
    new(ClaimTypes.Name, userName)
};

var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```

After login, only redirect to local URLs - otherwise `?ReturnUrl=https://evil.example` would work:

```csharp
return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
```

### User storage (`Services/UserService.cs`)

```csharp
userService.VerifyPassword(userName, password)

// Never store plain-text passwords
passwordHasher.HashPassword(userName, password)
passwordHasher.VerifyHashedPassword(userName, hash, password)
```

### Checking authentication

```csharp
[Authorize]
public IActionResult Secret() { ... }

User.Identity.Name
User.FindFirstValue(ClaimTypes.Name)
User.Claims
```

### Authorization - policy (`Program.cs`)

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "true"));
});

app.UseAuthentication();
app.UseAuthorization();   // order matters: authentication first
```

The admin gets the claim at login:

```csharp
if (userService.IsAdmin(userName))
{
    claims.Add(new Claim("IsAdmin", "true"));
}
```

### Checking authorization

```csharp
[Authorize(Policy = "AdminOnly")]
public IActionResult Admin() { ... }

User.HasClaim("IsAdmin", "true")
```

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Cookie authentication and the `AdminOnly` policy |
| `Services/UserService.cs` | In-memory users with hashed passwords |
| `Controllers/AccountController.cs` | Login (creates the claims and the cookie), logout, access denied |
| `Controllers/HomeController.cs` | Public, `[Authorize]` and policy-protected pages |
| `Views/Account/Login.cshtml` | The login form (posts back to the current URL, so `ReturnUrl` is kept) |
