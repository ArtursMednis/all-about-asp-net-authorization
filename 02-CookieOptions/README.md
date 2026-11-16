# 02 - Cookie options

A cookie-authentication app with every important `CookieAuthenticationOptions` setting spelled out: a custom scheme name, the cookie's name and security flags, how long a login lasts (with sliding expiration), "Remember me", and where users are redirected.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 02-CookieOptions
dotnet run --project CookieOptionsDemo
```

Open http://localhost:5002. Log in as `user` / `user` or `admin` / `admin`.

Keep the browser's developer tools open (**Application → Cookies**) to watch the cookie.

## What to try

1. Log in **without** "Remember me": the `MyAuthCookie` cookie is a session cookie (Expires = *Session*).
2. Log in **with** "Remember me": the cookie gets an expiry date and survives closing the browser.
3. Open **Secret**: it shows when the login was issued and when it expires. The login is valid for **1 minute** (short on purpose).
4. Reload **Secret** after ~30 seconds: sliding expiration issues a fresh cookie, and the next reload shows new dates.
5. Wait more than a minute without any request: you are logged out.

## Key code

### Scheme name

A scheme is just a name - use the same one everywhere.

```csharp
builder.Services.AddAuthentication("MyCookie")
    .AddCookie("MyCookie", options => { ... });

new ClaimsIdentity(claims, "MyCookie");
await HttpContext.SignInAsync("MyCookie", principal, properties);
await HttpContext.SignOutAsync("MyCookie");
```

### The cookie

```csharp
// Default: ".AspNetCore." + scheme name
options.Cookie.Name = "MyAuthCookie";

// No JavaScript access (default: true)
options.Cookie.HttpOnly = true;

// Use Always in production (default: SameAsRequest)
options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

// Not sent with most cross-site requests (default: Lax)
options.Cookie.SameSite = SameSiteMode.Lax;
```

### Lifetime

```csharp
options.ExpireTimeSpan = TimeSpan.FromMinutes(1);   // default: 14 days
options.SlidingExpiration = true;                   // renew after half the time has passed

// "Remember me"
await HttpContext.SignInAsync("MyCookie", principal,
    new AuthenticationProperties { IsPersistent = rememberMe });
```

A persistent cookie survives closing the browser. Either way, the login itself still expires after `ExpireTimeSpan`.

Reading the cookie's properties (issued, expires, persistent):

```csharp
var result = await HttpContext.AuthenticateAsync("MyCookie");
// result.Properties.IssuedUtc, ExpiresUtc, IsPersistent
```

### Redirects

```csharp
// These are the defaults
options.LoginPath = "/Account/Login";
options.AccessDeniedPath = "/Account/AccessDenied";
options.ReturnUrlParameter = "ReturnUrl";
```

Not logged in → `LoginPath`. Logged in but not allowed → `AccessDeniedPath`.

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | The `"MyCookie"` scheme and all cookie options |
| `Controllers/AccountController.cs` | Login with "Remember me" (`AuthenticationProperties.IsPersistent`) |
| `Controllers/HomeController.cs` | `Secret` reads the cookie's issued/expiry dates |
| `Services/UserService.cs` | In-memory users with hashed passwords |
