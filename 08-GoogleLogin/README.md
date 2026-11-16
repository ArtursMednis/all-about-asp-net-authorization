# 08 - Log in with Google

"Log in with Google" for an ASP.NET Core Identity app. The first time, a local user is created and linked to the Google account; after that it's a normal Identity login - same cookie, same claims, same policies. Local accounts with a password keep working.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`
- A Google account, to create OAuth credentials

## Set up Google

1. Open [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services** → **Credentials**.
2. **Create credentials** → **OAuth client ID** → Application type **Web application**.
   (If asked, configure the OAuth consent screen first.)
3. **Authorized redirect URIs**: `https://localhost:7208/signin-google`
4. Copy the client ID and client secret into the app's **user secrets** (kept outside the project folder, never committed):

```bash
cd 08-GoogleLogin/GoogleLogin
dotnet user-secrets set "Authentication:Google:ClientId" "<your client id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your client secret>"
```

(`appsettings.json` has the same keys, empty.) Without these settings the app still runs - the login page just says Google isn't configured.

## Run

```bash
cd 08-GoogleLogin
dotnet run --project GoogleLogin
```

Open https://localhost:7208. Local accounts: `user` / `user` and `admin` / `admin`.

## What to try

1. **Login** → **Log in with Google** → choose your Google account.
2. You are back and logged in. **Secret** shows *Password: no* and the linked Google login.
3. Log out and log in with Google again: the same local user, no new account.

## Key code

### Configuration (`Program.cs`)

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(...)
    .AddEntityFrameworkStores<AppDbContext>();

var google = builder.Configuration.GetSection("Authentication:Google");
if (!string.IsNullOrEmpty(google["ClientId"]))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = google["ClientId"]!;
            options.ClientSecret = google["ClientSecret"]!;
        });
}
```

Identity already registers an **external cookie** that briefly holds who Google says you are.

### Challenge (`Controllers/AccountController.cs`)

```csharp
// "Log in with Google" button
var callbackUrl = Url.Action("ExternalLoginCallback", new { returnUrl });
var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);
return Challenge(properties, provider);
```

The browser goes to Google, comes back to `/signin-google` (handled by the Google handler), then to our callback.

Buttons for all registered providers:

```csharp
ViewBag.ExternalProviders = await signInManager.GetExternalAuthenticationSchemesAsync();
```

### Callback

```csharp
var info = await signInManager.GetExternalLoginInfoAsync();

// Already linked to one of our users? Log in.
var result = await signInManager.ExternalLoginSignInAsync(
    info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
if (result.Succeeded)
{
    return Redirect(returnUrl);
}
```

### First login: create and link a user

```csharp
var email = info.Principal.FindFirstValue(ClaimTypes.Email);

var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
await userManager.CreateAsync(user);           // no password - Google login only
await userManager.AddLoginAsync(user, info);   // AspNetUserLogins: Google id -> our user
await signInManager.SignInAsync(user, isPersistent: false);
```

If a local account with that email already exists, the app does **not** link it silently - linking should be the user's decision while logged in.

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Identity + Google (only when configured) |
| `appsettings.json` | Empty `Authentication:Google` settings (use user secrets) |
| `Controllers/AccountController.cs` | Local login, external login, callback, first-login user creation |
| `Controllers/HomeController.cs` | Secret shows the linked logins |
