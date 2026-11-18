namespace NewsHub.Application.Tests.Features.Bookmarks.Commands;

using Microsoft.EntityFrameworkCore;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Bookmarks.Commands.AddBookmark;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class AddBookmarkCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private AddBookmarkCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new AddBookmarkCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenArticleNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var emptyArticles = new List<Article>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(emptyArticles.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddBookmarkCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenArticleNotPublished_ShouldThrowBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var draftArticle = new Article
        {
            Id = articleId,
            Title = "Bản nháp",
            Slug = "ban-nhap",
            ContentHtml = "<p>Nội dung nháp</p>",
            Status = ArticleStatus.Draft
        };

        var articles = new List<Article> { draftArticle }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new AddBookmarkCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleNotPublished, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenBookmarkAlreadyExists_ShouldReturnWithoutSavingDuplicate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var article = new Article
        {
            Id = articleId,
            Title = "Tin xuất bản",
            Slug = "tin-xuat-ban",
            ContentHtml = "<p>Nội dung bài viết</p>",
            Status = ArticleStatus.Published
        };

        var existingBookmark = new UserBookmark
        {
            UserId = userId,
            ArticleId = articleId
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        var bookmarks = new List<UserBookmark> { existingBookmark }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(bookmarks.Object);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new AddBookmarkCommand(articleId), CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldAddBookmarkSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var article = new Article
        {
            Id = articleId,
            Title = "Tin mới xuất bản",
            Slug = "tin-moi-xuat-ban",
            ContentHtml = "<p>Nội dung bài viết</p>",
            Status = ArticleStatus.Published
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        var bookmarks = new List<UserBookmark>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(bookmarks.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new AddBookmarkCommand(articleId), CancellationToken.None);

        // Assert
        bookmarks.Verify(b => b.Add(It.Is<UserBookmark>(x => x.UserId == userId && x.ArticleId == articleId)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
