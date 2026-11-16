using AuthorizationDemo.Models;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationDemo.AuthorizationRules;

// Resource-based authorization: the answer depends on WHICH document,
// so it can't be an attribute on the action - the document isn't loaded yet.
public class EditDocumentRequirement : IAuthorizationRequirement
{
}

// Handler 1: the owner may edit their own document.
public class DocumentOwnerHandler : AuthorizationHandler<EditDocumentRequirement, Document>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EditDocumentRequirement requirement, Document document)
    {
        if (document.Owner == context.User.Identity?.Name)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

// Handler 2: admins may edit any document.
// Several handlers for one requirement work like OR - one success is enough.
public class AdminCanEditAnyDocumentHandler : AuthorizationHandler<EditDocumentRequirement, Document>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EditDocumentRequirement requirement, Document document)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
