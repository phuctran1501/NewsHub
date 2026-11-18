namespace NewsHub.Application.Tests.Features.ReadingHistory.Queries;

using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.ReadingHistory.Queries.GetMyReadingHistory;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetMyReadingHistoryQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetMyReadingHistoryQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyReadingHistoryQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticated_ShouldReturnPagedReadingHistories()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Tin đã xem",
            Slug = "tin-da-xem",
            ContentHtml = "<p>Nội dung</p>"
        };

        var history = new ReadingHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ArticleId = article.Id,
            Article = article,
            ReadAt = DateTimeOffset.UtcNow
        };

        var histories = new List<ReadingHistory> { history }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyReadingHistoryQuery(1, 10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(article.Title, result.Items[0].Title);
        Assert.Equal(history.Id, result.Items[0].HistoryId);
    }
}
