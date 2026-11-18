namespace NewsHub.Application.Features.Articles.Commands.ArchiveArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record ArchiveArticleCommand(Guid Id) : IRequest<ArticleDetailDto>;

public sealed class ArchiveArticleCommandValidator : AbstractValidator<ArchiveArticleCommand>
{
    public ArchiveArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class ArchiveArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICacheService cacheService,
    INotificationService notificationService) : IRequestHandler<ArchiveArticleCommand, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(
        ArchiveArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để lưu trữ bài viết.", ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .Include(a => a.CoverImage)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var isAuthor = article.AuthorId == currentUser.Id;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền lưu trữ bài viết của tác giả khác.", ErrorCodes.ArticleForbiddenEdit);
        }

        article.Status = ArticleStatus.Archived;
        article.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa Cache Redis
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(article.Slug), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleDetail(article.Id), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleRelated(article.Id), cancellationToken);

        // Phát tín hiệu SignalR
        await notificationService.NotifyArticleStatusChangedAsync(
            article.AuthorId,
            article.Id,
            article.Status.ToString(),
            cancellationToken);

        var categories = article.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();

        return new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.ContentHtml,
            article.Status.ToString(),
            new AuthorSummaryDto(article.Author!.Id, article.Author.FullName, article.Author.AvatarUrl),
            article.CompanyId,
            article.SourceName,
            article.SourceUrl,
            article.CoverImage is null ? null : new CoverImageDto(
                article.CoverImage.Id,
                article.CoverImage.Url,
                article.CoverImage.Width,
                article.CoverImage.Height,
                article.CoverImage.Format),
            article.ReadingTimeMinutes,
            article.ViewCount,
            categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt);
    }
}
