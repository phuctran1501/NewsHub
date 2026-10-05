namespace NewsHub.Domain.Interfaces;

public interface IHasUpdatedAt
{
    DateTimeOffset? UpdatedAt { get; set; }
}
