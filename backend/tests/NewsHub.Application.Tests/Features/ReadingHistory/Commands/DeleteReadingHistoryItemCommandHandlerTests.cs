namespace NewsHub.Application.Tests.Features.ReadingHistory.Commands;

using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.ReadingHistory.Commands.DeleteReadingHistoryItem;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class DeleteReadingHistoryItemCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private DeleteReadingHistoryItemCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new DeleteReadingHistoryItemCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenHistoryNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var emptyHistories = new List<ReadingHistory>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(emptyHistories.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteReadingHistoryItemCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.ReadingHistoryNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenHistoryBelongsToDifferentUser_ShouldThrowNotFoundException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(currentUserId);

        var otherHistory = new ReadingHistory
        {
            Id = historyId,
            UserId = otherUserId,
            ArticleId = Guid.NewGuid()
        };

        var histories = new List<ReadingHistory> { otherHistory }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteReadingHistoryItemCommand(historyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.ReadingHistoryNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRemoveAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var history = new ReadingHistory
        {
            Id = historyId,
            UserId = userId,
            ArticleId = Guid.NewGuid()
        };

        var histories = new List<ReadingHistory> { history }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteReadingHistoryItemCommand(historyId), CancellationToken.None);

        // Assert
        histories.Verify(h => h.Remove(history), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
