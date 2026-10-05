namespace NewsHub.Domain.Interfaces;

public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}
