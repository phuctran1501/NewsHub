namespace NewsHub.Application.Tests.Features.Bookmarks.Commands;

using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Bookmarks.Commands.RemoveBookmark;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RemoveBookmarkCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private RemoveBookmarkCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new RemoveBookmarkCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenBookmarkDoesNotExist_ShouldReturnWithoutError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var emptyBookmarks = new List<UserBookmark>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(emptyBookmarks.Object);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RemoveBookmarkCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBookmarkExists_ShouldRemoveAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var existingBookmark = new UserBookmark
        {
            UserId = userId,
            ArticleId = articleId
        };

        var bookmarks = new List<UserBookmark> { existingBookmark }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(bookmarks.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new RemoveBookmarkCommand(articleId), CancellationToken.None);

        // Assert
        bookmarks.Verify(b => b.Remove(existingBookmark), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
