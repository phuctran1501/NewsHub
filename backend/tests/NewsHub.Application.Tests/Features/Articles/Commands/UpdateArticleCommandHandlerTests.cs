namespace NewsHub.Application.Tests.Features.Articles.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.UpdateArticle;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Xunit;

public class UpdateArticleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IHtmlSanitizer> _htmlSanitizerMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private UpdateArticleCommandHandler CreateHandler()
    {
        _htmlSanitizerMock
            .Setup(s => s.Sanitize(It.IsAny<string>()))
            .Returns<string>(html => html);

        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        _dbContextMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new UpdateArticleCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _htmlSanitizerMock.Object,
            _cacheServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCompanyMemberEditsPublishedCompanyArticle_ShouldRevertToPendingCompanyReviewAndPreserveSlug()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Phóng Viên A", Email = "pv@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            CompanyId = companyId,
            Title = "Tiêu đề cũ",
            Slug = "tieu-de-cu",
            ContentHtml = "<p>Nội dung cũ</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        var members = new List<CompanyMember>
        {
            new() { CompanyId = companyId, UserId = authorId, Role = CompanyMemberRole.Member }
        }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new UpdateArticleCommand(articleId, Title: "Tiêu đề mới", ContentHtml: "<p>Nội dung mới cập nhật</p>"),
            CancellationToken.None);

        // Assert
        Assert.Equal("PendingCompanyReview", result.Status);
        Assert.Equal("tieu-de-cu", result.Slug);
        Assert.Equal("Tiêu đề mới", result.Title);
    }

    [Fact]
    public async Task Handle_WhenCompanyOwnerEditsPublishedCompanyArticle_ShouldRemainPublished()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var owner = new User { Id = ownerId, FullName = "Tổng Biên Tập", Email = "owner@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = ownerId,
            Author = owner,
            CompanyId = companyId,
            Title = "Tiêu đề gốc",
            Slug = "tieu-de-goc",
            ContentHtml = "<p>Nội dung gốc</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(ownerId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        var members = new List<CompanyMember>
        {
            new() { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner }
        }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new UpdateArticleCommand(articleId, ContentHtml: "<p>Nội dung đã hiệu đính</p>"),
            CancellationToken.None);

        // Assert
        Assert.Equal("Published", result.Status);
    }

    [Fact]
    public async Task Handle_WhenJournalistEditsPublishedFreelanceArticle_ShouldRemainPublished()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo Tự Do", Email = "freelance@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            CompanyId = null,
            Title = "Bài tự do",
            Slug = "bai-tu-do",
            ContentHtml = "<p>Nội dung tự do</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow.AddHours(-3)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new UpdateArticleCommand(articleId, ContentHtml: "<p>Nội dung tự do sửa lại</p>"),
            CancellationToken.None);

        // Assert
        Assert.Equal("Published", result.Status);
    }
}
