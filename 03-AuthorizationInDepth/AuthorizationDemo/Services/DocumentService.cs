using AuthorizationDemo.Models;

namespace AuthorizationDemo.Services;

// A few documents in memory, each with an owner.
public class DocumentService
{
    private readonly List<Document> documents =
    [
        new() { Id = 1, Owner = "user", Title = "User's notes", Content = "Only user (or an admin) may edit this." },
        new() { Id = 2, Owner = "admin", Title = "Admin's plan", Content = "Only admin may edit this." },
    ];

    public IReadOnlyList<Document> GetAll() => documents;

    public Document? Find(int id) => documents.FirstOrDefault(d => d.Id == id);
}
