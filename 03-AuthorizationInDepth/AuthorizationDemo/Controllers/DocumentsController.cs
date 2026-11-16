using AuthorizationDemo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationDemo.Controllers;

public class DocumentsController(DocumentService documentService, IAuthorizationService authorizationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        // The same check is used to decide which "Edit" links to show.
        var documents = new List<(Models.Document Document, bool CanEdit)>();
        foreach (var document in documentService.GetAll())
        {
            var result = await authorizationService.AuthorizeAsync(User, document, "EditDocument");
            documents.Add((document, result.Succeeded));
        }
        return View(documents);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var document = documentService.Find(id);
        if (document == null)
        {
            return NotFound();
        }

        // Resource-based check: load the document first, then ask "may this user edit THIS document?"
        var result = await authorizationService.AuthorizeAsync(User, document, "EditDocument");
        if (!result.Succeeded)
        {
            return Forbid(); // -> /Account/AccessDenied
        }

        return View(document);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string content)
    {
        var document = documentService.Find(id);
        if (document == null)
        {
            return NotFound();
        }

        // Check again on save - hiding the link is not enough.
        var result = await authorizationService.AuthorizeAsync(User, document, "EditDocument");
        if (!result.Succeeded)
        {
            return Forbid();
        }

        document.Title = title;
        document.Content = content;
        return RedirectToAction("Index");
    }
}
