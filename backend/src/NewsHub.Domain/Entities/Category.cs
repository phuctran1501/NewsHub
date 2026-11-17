namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Interfaces;

public class Category : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
