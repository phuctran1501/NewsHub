namespace NewsHub.Application.Features.Articles.Commands.PublishArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record PublishArticleCommand(Guid Id) : IRequest<ArticleDetailDto>;

public sealed class PublishArticleCommandValidator : AbstractValidator<PublishArticleCommand>
{
    public PublishArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class PublishArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICacheService cacheService,
    INotificationService notificationService,
    ILogger<PublishArticleCommandHandler> logger,
    IBackgroundJobService? backgroundJobService = null) : IRequestHandler<PublishArticleCommand, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(
        PublishArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xuất bản bài viết.", ErrorCodes.Unauthorized);
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
            throw new ForbiddenException("Bạn không có quyền xuất bản bài viết của tác giả khác.", ErrorCodes.ArticleForbiddenEdit);
        }

        if (article.Status == ArticleStatus.Published)
        {
            var existingCats = article.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();
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
                existingCats.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
                article.PublishedAt,
                article.CreatedAt,
                article.UpdatedAt);
        }

        article.Status = ArticleStatus.Published;
        article.PublishedAt = DateTimeOffset.UtcNow;
        article.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa Cache Redis
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(article.Slug), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleDetail(article.Id), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleRelated(article.Id), cancellationToken);

        try
        {
            // Phát tín hiệu SignalR thời gian thực tới tác giả và phòng theo dõi bài viết
            await notificationService.NotifyArticleStatusChangedAsync(
                article.AuthorId,
                article.Id,
                article.Status.ToString(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi khi phát tín hiệu thông báo xuất bản bài viết {ArticleId}", article.Id);
        }

        try
        {
            // Kích hoạt tác vụ nền Hangfire xử lý tóm tắt AI và sinh vector nhúng
            backgroundJobService?.Enqueue<IAiBackgroundJobs>(job =>
                job.GenerateArticleSummaryAsync(article.Id, CancellationToken.None));
            backgroundJobService?.Enqueue<IAiBackgroundJobs>(job =>
                job.GenerateArticleEmbeddingAsync(article.Id, CancellationToken.None));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi khi đưa tác vụ tóm tắt AI / Vector vào hàng đợi cho bài viết {ArticleId}", article.Id);
        }

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
