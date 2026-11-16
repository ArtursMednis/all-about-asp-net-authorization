# 07 - Identity features: registration, lockout, two-factor

Three features ASP.NET Core Identity gives you almost for free:

- **Registration with email confirmation** - no login until the user clicks a link from an email.
- **Account lockout** - three wrong passwords in a row lock the account for a minute.
- **Two-factor authentication (2FA)** with an authenticator app (Microsoft Authenticator, Google Authenticator, 1Password, ...).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An authenticator app on your phone (for 2FA)

## Run

```bash
cd 07-IdentityFeatures
dotnet run --project IdentityFeatures
```

Open http://localhost:5207. Existing users: `user` / `user` and `admin` / `admin` (emails already confirmed).

**Emails are printed to the app's console window** - there is no real email sending.

## What to try

**Registration**
1. **Register** with any email and a password of at least 4 characters.
2. Try to log in: *Please confirm your email address first.*
3. Copy the confirmation link from the console window and open it. Now you can log in.

**Lockout**
1. Log in as `user` with a wrong password three times.
2. The account is locked - even the right password is refused for a minute.

**Two-factor authentication**
1. Log in and open **Two-factor**.
2. Add an account in your authenticator app with the key shown (choose "enter a setup key").
3. Enter the 6-digit code from the app → 2FA is **on**.
4. Log out and log in again: after the password, the app asks for a code.

## Key code

### Configuration (`Program.cs`)

```csharp
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;

        options.Lockout.MaxFailedAccessAttempts = 3;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();   // email confirmation tokens and authenticator codes
```

### Registration (`Controllers/AccountController.cs`)

```csharp
var user = new IdentityUser { UserName = email, Email = email };
await userManager.CreateAsync(user, password);

var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
var link = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
emailSender.Send(email, "Confirm your email", $"Click to confirm: {link}");

// When the link is opened:
await userManager.ConfirmEmailAsync(user, token);
```

### Login with lockout and 2FA

```csharp
var result = await signInManager.PasswordSignInAsync(
    userName, password, isPersistent: false, lockoutOnFailure: true);

result.Succeeded          // logged in
result.RequiresTwoFactor  // password OK, now ask for the code
result.IsLockedOut        // too many wrong passwords
result.IsNotAllowed       // email not confirmed
```

Second step (`LoginWith2fa`):

```csharp
var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
    code, isPersistent: false, rememberClient: false);
```

### Two-factor setup (`Controllers/TwoFactorController.cs`)

```csharp
// The secret shared with the authenticator app
var key = await userManager.GetAuthenticatorKeyAsync(user);
if (key == null)
{
    await userManager.ResetAuthenticatorKeyAsync(user);
    key = await userManager.GetAuthenticatorKeyAsync(user);
}

// Authenticator apps read this from a QR code (or you type the key in)
var uri = $"otpauth://totp/AuthTutorials:{userName}?secret={key}&issuer=AuthTutorials&digits=6";

// Turn it on only after the app produced a valid code
var valid = await userManager.VerifyTwoFactorTokenAsync(
    user, userManager.Options.Tokens.AuthenticatorTokenProvider, code);
if (valid)
{
    await userManager.SetTwoFactorEnabledAsync(user, true);
}
```

## Not covered here

- Password reset - same idea as email confirmation, with `GeneratePasswordResetTokenAsync` / `ResetPasswordAsync`.
- Recovery codes for 2FA - `GenerateNewTwoFactorRecoveryCodesAsync`.
- Passkeys.

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Identity options: confirmed email, lockout, token providers |
| `Services/EmailSender.cs` | Fake email sender (writes to the console) |
| `Controllers/AccountController.cs` | Register, confirm email, login (lockout, 2FA), second step |
| `Controllers/TwoFactorController.cs` | Turn 2FA on/off with an authenticator app |
| `Data/SeedData.cs` | Demo users with confirmed emails |
