namespace NewsHub.Application.Tests.Features.Bookmarks.Queries;

using Moq;
using MockQueryable;
using MockQueryable.Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Bookmarks.Queries.GetMyBookmarks;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetMyBookmarksQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetMyBookmarksQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyBookmarksQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserAuthenticated_ShouldReturnPagedBookmarks()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var author = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Tác giả X",
            Email = "author@newshub.vn",
            PasswordHash = "hashed"
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Thời sự",
            Slug = "thoi-su"
        };

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Tin số 1",
            Slug = "tin-so-1",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            Author = author,
            ArticleCategories = new List<ArticleCategory>
            {
                new() { Category = category }
            }
        };

        var bookmark = new UserBookmark
        {
            UserId = userId,
            ArticleId = article.Id,
            Article = article,
            SavedAt = DateTimeOffset.UtcNow
        };

        var bookmarks = new List<UserBookmark> { bookmark }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(bookmarks.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyBookmarksQuery(1, 10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(article.Title, result.Items[0].Title);
        Assert.Equal(author.FullName, result.Items[0].AuthorName);
        Assert.Equal(category.Name, result.Items[0].CategoryName);
    }
}
