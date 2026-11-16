using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Security.Claims;

namespace AuthorizationDemo.AuthorizationRules;

// A requirement is just data: "the user must be at least N years old".
public record MinimumAgeRequirement(int MinimumAge) : IAuthorizationRequirement;

// A handler decides whether a requirement is met.
// When a rule doesn't fit RequireClaim/RequireRole, this is where it goes.
public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumAgeRequirement requirement)
    {
        var claim = context.User.FindFirst(ClaimTypes.DateOfBirth);
        if (claim == null)
        {
            // Not calling Succeed means "not met" - access is denied unless another handler succeeds.
            return Task.CompletedTask;
        }

        var dateOfBirth = DateOnly.Parse(claim.Value, CultureInfo.InvariantCulture);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age))
        {
            age--; // no birthday yet this year
        }

        if (age >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
