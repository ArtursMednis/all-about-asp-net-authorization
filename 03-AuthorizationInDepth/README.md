# 03 - Authorization in depth

Four ways to decide what a logged-in user may do, in one cookie-authentication app:

1. **Fallback policy** - every page requires a login unless it says `[AllowAnonymous]`.
2. **Roles** - which are just claims of type `role`.
3. **A custom requirement and handler** - "at least 18 years old", calculated from a date-of-birth claim.
4. **Resource-based authorization** - you may edit a document if you own it, or if you are an admin.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Run

```bash
cd 03-AuthorizationInDepth
dotnet run --project AuthorizationDemo
```

Open http://localhost:5203.

| User | Password | Roles | Age |
|---|---|---|---|
| `user` | `user` | Employee | 14 |
| `admin` | `admin` | Employee, Admin | adult |

## What to try

| Page | Rule | `user` | `admin` |
|---|---|---|---|
| Secret | fallback policy (any logged-in user) | yes | yes |
| Admin | `[Authorize(Roles = "Admin")]` | no | yes |
| Adults | custom requirement: at least 18 | no | yes |
| Documents | owner or admin may edit | own document only | all |

As `user`, open `/Documents/Edit/2` by hand: *Access denied* - the check runs on the server, hiding the link is not enough.

## Key code

### Fallback policy (`Program.cs`)

```csharp
options.FallbackPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
```

It applies to every endpoint **without** an authorization attribute. Public pages opt out:

```csharp
[AllowAnonymous]   // the home page, and the whole AccountController
public IActionResult Index() ...
```

### Roles

A role is a claim of type `ClaimTypes.Role` - one claim per role:

```csharp
claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
```

Three ways to check it:

```csharp
[Authorize(Roles = "Admin")]
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
User.IsInRole("Admin")
```

### Custom requirement (`AuthorizationRules/MinimumAge.cs`)

```csharp
public record MinimumAgeRequirement(int MinimumAge) : IAuthorizationRequirement;

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        // read the date of birth claim, calculate the age...
        if (age >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;   // not calling Succeed = not met
    }
}
```

```csharp
options.AddPolicy("AtLeast18", policy => policy.Requirements.Add(new MinimumAgeRequirement(18)));
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();

[Authorize(Policy = "AtLeast18")]
```

### Resource-based authorization (`Controllers/DocumentsController.cs`)

The answer depends on *which* document, so it can't be an attribute - load the document first, then ask:

```csharp
var result = await authorizationService.AuthorizeAsync(User, document, "EditDocument");
if (!result.Succeeded)
{
    return Forbid();
}
```

The handlers receive the document too (`AuthorizationRules/EditDocument.cs`):

```csharp
public class DocumentOwnerHandler : AuthorizationHandler<EditDocumentRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, EditDocumentRequirement requirement, Document document)
    {
        if (document.Owner == context.User.Identity?.Name)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
```

A second handler (`AdminCanEditAnyDocumentHandler`) succeeds for admins. Several handlers for one requirement work like **OR** - one success is enough.

## Project structure

| File | What it does |
|---|---|
| `Program.cs` | Fallback policy, the policies, handler registration |
| `AuthorizationRules/MinimumAge.cs` | Custom requirement + handler |
| `AuthorizationRules/EditDocument.cs` | Resource-based requirement + two handlers |
| `Controllers/AccountController.cs` | Login: name, date of birth and role claims |
| `Controllers/HomeController.cs` | Secret (fallback), Admin (role), Adults (custom requirement) |
| `Controllers/DocumentsController.cs` | `IAuthorizationService` with a document |
| `Services/UserService.cs`, `Services/DocumentService.cs` | In-memory users and documents |
