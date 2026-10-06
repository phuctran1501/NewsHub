namespace NewsHub.Domain.Interfaces;

public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}

public interface IHasUpdatedAt
{
    DateTimeOffset? UpdatedAt { get; set; }
}