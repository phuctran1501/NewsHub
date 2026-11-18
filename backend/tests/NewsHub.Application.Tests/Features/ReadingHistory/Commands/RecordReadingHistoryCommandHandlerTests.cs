namespace NewsHub.Application.Tests.Features.ReadingHistory.Commands;

using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.ReadingHistory.Commands.RecordReadingHistory;
using NewsHub.Domain.Entities;
using Xunit;

public class RecordReadingHistoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILogger<RecordReadingHistoryCommandHandler>> _loggerMock = new();

    private RecordReadingHistoryCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldDoNothing()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act
        await handler.Handle(new RecordReadingHistoryCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticatedAndNewRead_ShouldAddNewHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var histories = new List<ReadingHistory>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RecordReadingHistoryCommand(articleId), CancellationToken.None);

        // Assert
        histories.Verify(h => h.Add(It.Is<ReadingHistory>(x => x.UserId == userId && x.ArticleId == articleId)), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticatedAndAlreadyRead_ShouldUpdateReadAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var initialTime = DateTimeOffset.UtcNow.AddHours(-1);
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var existingHistory = new ReadingHistory
        {
            UserId = userId,
            ArticleId = articleId,
            ReadAt = initialTime
        };

        var histories = new List<ReadingHistory> { existingHistory }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RecordReadingHistoryCommand(articleId), CancellationToken.None);

        // Assert
        Assert.True(existingHistory.ReadAt > initialTime);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
