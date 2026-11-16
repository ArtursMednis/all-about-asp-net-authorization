# All about ASP.NET Core authentication and authorization

Seventeen small, runnable ASP.NET Core projects. Each one focuses on **a single topic** - from a cookie login with a few lines of code to OAuth 2.0, OpenID Connect and a Backend for Frontend - with as little noise as possible: no templates, no CSS frameworks, just the code that matters.

Every tutorial is **self-contained**: its own folder, its own solution, its own README with how to run it, what to try, and the key code. Pick any one - you don't need to go through the others first.

## Tutorials

### Part 1 - Local authentication

| # | Tutorial | What you learn |
|---|---|---|
| 01 | [Cookie authentication](01-CookieAuth) | Login with claims and a cookie, `[Authorize]`, a first policy |
| 02 | [Cookie options](02-CookieOptions) | Scheme names, cookie security flags, lifetime, sliding expiration, "Remember me" |
| 03 | [Authorization in depth](03-AuthorizationInDepth) | Fallback policy, roles, custom requirements and handlers, resource-based authorization |
| 04 | [SPA with cookie authentication](04-SpaCookieAuth) | React, Angular and Vue with an HttpOnly cookie, route guards, dev proxy |
| 05 | [ASP.NET Core Identity](05-Identity) | `UserManager`, `SignInManager`, users and claims in a database |
| 06 | [Extending IdentityUser](06-ExtendingIdentityUser) | Custom user properties |
| 07 | [Identity features](07-IdentityFeatures) | Registration with email confirmation, lockout, two-factor authentication |
| 08 | [Log in with Google](08-GoogleLogin) | External login with Identity |

### Part 2 - APIs and tokens

| # | Tutorial | What you learn |
|---|---|---|
| 09 | [Custom authentication handler](09-ApiKeyAuth) | API keys - how authentication handlers work inside |
| 10 | [Token authentication (JWT)](10-TokenAuth) | Issuing and fully validating JWTs, scopes, a console client |
| 11 | [Multiple schemes](11-MultipleSchemes) | Cookie and JWT in one app, policy schemes |
| 12 | [Client credentials with OpenIddict](12-OpenIddictClientCredentials) | Authorization server, resource API and client as separate apps |
| 13 | [Client credentials with Duende IdentityServer](13-DuendeClientCredentials) | The same with Duende IdentityServer |

### Part 3 - Delegated login (OpenID Connect)

| # | Tutorial | What you learn |
|---|---|---|
| 14 | [Authorization code flow with OpenIddict](14-OpenIddictAuthCode) | Login through an auth server, PKCE, ID token, userinfo |
| 15 | [Authorization code flow with Duende IdentityServer](15-DuendeAuthCode) | The same with Duende IdentityServer |
| 16 | [Backend for Frontend](16-Bff) | A SPA without tokens in the browser, reverse proxy, token refresh |
| 17 | [Back-channel logout](17-BackChannelLogout) | Ending sessions in every app when the auth server session ends |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 22 or newer - only for the single-page apps (04 and 16)
- A trusted HTTPS development certificate - for the tutorials that run on HTTPS:

```bash
dotnet dev-certs https --trust
```

Each tutorial runs with `dotnet run --project <ProjectName>` from its folder, or by opening its `.slnx` file in Visual Studio 2022 (17.13+), Rider or VS Code. Tutorials with several projects explain the start order in their README.

## Conventions

The tutorials share the same small vocabulary, so the differences between them stand out:

- **Demo users:** `user` / `user` and `admin` / `admin`. API clients: `client` / `client-secret` and `admin-client` / `admin-secret`.
- **Pages / endpoints:** a public page, a *Secret* page for any logged-in user, and an *Admin* page.
- **Authorization:** an `AdminOnly` policy - usually based on an `IsAdmin` claim (or the `admin` scope for API clients).
- **Storage:** in memory (or the EF Core in-memory database), so every project starts clean and needs no setup.
- **Each project uses its own port**, so several tutorials can run at the same time.

## Notes

- The code is written for learning: passwords are relaxed, keys and secrets are in config files or code, and keys are generated on startup. Each README points out what to do differently in production.
- **Duende IdentityServer** (tutorials 13, 15, 16, 17) is free for development and testing; production use requires a [license](https://duendesoftware.com/products/identityserver). Without a license key it logs a warning at startup.
- **OpenIddict** (tutorials 12 and 14) is free and open source.
