namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.Commands.ReviewEditorialArticle;
using NewsHub.Application.Features.Companies.Commands.SubmitArticleToCompany;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class EditorialWorkflowCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<SubmitArticleToCompanyCommandHandler>> _submitLoggerMock = new();
    private readonly Mock<ILogger<ReviewEditorialArticleCommandHandler>> _reviewLoggerMock = new();

    [Fact]
    public async Task SubmitArticle_WhenNotAuthor_ShouldThrowForbiddenException()
    {
        var authorId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(callerId);

        var article = new Article { Id = articleId, AuthorId = authorId, Title = "T", Slug = "s", ContentHtml = "c" };
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article> { article }.BuildMockDbSet().Object);

        var handler = new SubmitArticleToCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _submitLoggerMock.Object);

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new SubmitArticleToCompanyCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleForbiddenEdit, ex.ErrorCode);
    }

    [Fact]
    public async Task SubmitArticle_WhenNoCompany_ShouldThrowBadRequestException()
    {
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(authorId);

        var article = new Article { Id = articleId, AuthorId = authorId, CompanyId = null, Title = "T", Slug = "s", ContentHtml = "c" };
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article> { article }.BuildMockDbSet().Object);

        var handler = new SubmitArticleToCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _submitLoggerMock.Object);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new SubmitArticleToCompanyCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleHasNoCompany, ex.ErrorCode);
    }

    [Fact]
    public async Task SubmitArticle_WhenValid_ShouldSetPendingCompanyReviewAndSave()
    {
        var authorId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(authorId);

        var article = new Article { Id = articleId, AuthorId = authorId, CompanyId = companyId, Title = "T", Slug = "s", ContentHtml = "c", Status = ArticleStatus.Draft };
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article> { article }.BuildMockDbSet().Object);

        var member = new CompanyMember { CompanyId = companyId, UserId = authorId, Role = CompanyMemberRole.Member };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { member }.BuildMockDbSet().Object);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var handler = new SubmitArticleToCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _submitLoggerMock.Object);

        await handler.Handle(new SubmitArticleToCompanyCommand(articleId), CancellationToken.None);

        Assert.Equal(ArticleStatus.PendingCompanyReview, article.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(ownerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewArticle_WhenOwnerApproves_ShouldSetPublishedAndInvalidateCache()
    {
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var ownerMember = new CompanyMember { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { ownerMember }.BuildMockDbSet().Object);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var article = new Article { Id = articleId, AuthorId = authorId, CompanyId = companyId, Title = "T", Slug = "slug-1", ContentHtml = "c", Status = ArticleStatus.PendingCompanyReview };
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article> { article }.BuildMockDbSet().Object);

        var handler = new ReviewEditorialArticleCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _cacheMock.Object, _notifMock.Object, _reviewLoggerMock.Object);

        await handler.Handle(new ReviewEditorialArticleCommand(companyId, articleId, "Approve"), CancellationToken.None);

        Assert.Equal(ArticleStatus.Published, article.Status);
        Assert.NotNull(article.PublishedAt);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _notifMock.Verify(n => n.SendToGroupAsync($"Company_{companyId}", It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewArticle_WhenOwnerRequestsChanges_ShouldSetDraft()
    {
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var ownerMember = new CompanyMember { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { ownerMember }.BuildMockDbSet().Object);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var article = new Article { Id = articleId, AuthorId = authorId, CompanyId = companyId, Title = "T", Slug = "slug-1", ContentHtml = "c", Status = ArticleStatus.PendingCompanyReview };
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article> { article }.BuildMockDbSet().Object);

        var handler = new ReviewEditorialArticleCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _cacheMock.Object, _notifMock.Object, _reviewLoggerMock.Object);

        await handler.Handle(new ReviewEditorialArticleCommand(companyId, articleId, "RequestChanges", "Sửa lỗi chính tả."), CancellationToken.None);

        Assert.Equal(ArticleStatus.Draft, article.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(authorId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
