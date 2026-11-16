namespace AuthorizationDemo.Models;

public class Document
{
    public int Id { get; init; }
    public required string Owner { get; init; }
    public required string Title { get; set; }
    public required string Content { get; set; }
}
