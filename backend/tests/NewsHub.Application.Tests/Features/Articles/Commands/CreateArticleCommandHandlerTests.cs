namespace NewsHub.Application.Tests.Features.Articles.Commands;

using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.CreateArticle;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class CreateArticleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IHtmlSanitizer> _htmlSanitizerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private CreateArticleCommandHandler CreateHandler() =>
        new(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _htmlSanitizerMock.Object,
            _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateArticleSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var author = new User { Id = userId, FullName = "Nhà Báo C", Email = "c@newshub.vn", PasswordHash = "hash" };
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Kinh tế", Slug = "kinh-te" };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var users = new List<User> { author }.BuildMockDbSet();
        var categories = new List<Category> { category }.BuildMockDbSet();
        var articles = new List<Article>().BuildMockDbSet();
        var mediaFiles = new List<MediaFile>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaFiles.Object);

        _htmlSanitizerMock
            .Setup(h => h.Sanitize(It.IsAny<string>()))
            .Returns("<p>Nội dung sạch và an toàn cho độc giả</p>");

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var handler = CreateHandler();
        var command = new CreateArticleCommand(
            "Tiêu đề kinh tế hôm nay",
            "<p>Nội dung chưa làm sạch</p>",
            null,
            new List<Guid> { categoryId });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tiêu đề kinh tế hôm nay", result.Title);
        Assert.Equal("Draft", result.Status);
        Assert.Equal(1, result.ReadingTimeMinutes);
        Assert.Single(result.Categories);
        Assert.Equal("Kinh tế", result.Categories[0].Name);
        _dbContextMock.Verify(db => db.Articles.Add(It.IsAny<Article>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonExistentCategoryProvided_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var author = new User { Id = userId, FullName = "Nhà Báo C", Email = "c@newshub.vn", PasswordHash = "hash" };
        var nonExistentCatId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var users = new List<User> { author }.BuildMockDbSet();
        var categories = new List<Category>().BuildMockDbSet(); // empty

        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article>().BuildMockDbSet().Object);

        _htmlSanitizerMock.Setup(h => h.Sanitize(It.IsAny<string>())).Returns("<p>Nội dung</p>");

        var handler = CreateHandler();
        var command = new CreateArticleCommand("Tiêu đề bài viết hợp lệ", "<p>Nội dung dài đủ 20 ký tự</p>", null, new List<Guid> { nonExistentCatId });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.CategoryNotFound, ex.ErrorCode);
    }
}
