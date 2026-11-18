namespace NewsHub.Application.Features.Articles.Queries.GetArticleBySlug;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.RecordArticleView;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetArticleBySlugQuery(string Slug) : IRequest<ArticleDetailDto>;

public sealed class GetArticleBySlugQueryValidator : AbstractValidator<GetArticleBySlugQuery>
{
    public GetArticleBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Đường dẫn (slug) bài viết không được để trống.");
    }
}

public sealed class GetArticleBySlugQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICurrentUser currentUser,
    ISender sender,
    ILogger<GetArticleBySlugQueryHandler> logger) : IRequestHandler<GetArticleBySlugQuery, ArticleDetailDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<ArticleDetailDto> Handle(
        GetArticleBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var cleanSlug = request.Slug.Trim().ToLowerInvariant();
        var isGuid = Guid.TryParse(cleanSlug, out var articleGuid);
        var cacheKey = isGuid
            ? ArticleCacheKeys.ArticleDetail(articleGuid)
            : ArticleCacheKeys.ArticleSlug(cleanSlug);

        // 1. Đọc từ Cache-Aside Redis nếu đã có (chỉ cache bài viết Published)
        var cached = await cacheService.GetAsync<ArticleDetailDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            await RecordInteractionsAsync(cached.Id, cancellationToken);
            return cached;
        }

        // 2. Truy vấn Database
        var article = await dbContext.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.CoverImage)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .FirstOrDefaultAsync(a => a.Slug.ToLower() == cleanSlug || (isGuid && a.Id == articleGuid), cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        // 3. Kiểm tra quyền xem bài chưa xuất bản
        if (article.Status != ArticleStatus.Published)
        {
            var isAuthor = currentUser.IsAuthenticated && article.AuthorId == currentUser.Id;
            var isAdmin = currentUser.IsAuthenticated && currentUser.IsInRole(UserRole.Admin);

            if (!isAuthor && !isAdmin)
            {
                throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
            }
        }

        var author = article.Author!;
        var cover = article.CoverImage;
        var categories = article.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();

        var detailDto = new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.ContentHtml,
            article.Status.ToString(),
            new AuthorSummaryDto(author.Id, author.FullName, author.AvatarUrl),
            article.CompanyId,
            article.SourceName,
            article.SourceUrl,
            cover is null ? null : new CoverImageDto(cover.Id, cover.Url, cover.Width, cover.Height, cover.Format),
            article.ReadingTimeMinutes,
            article.ViewCount,
            categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt);

        // 4. Lưu Cache nếu bài viết đã Published
        if (article.Status == ArticleStatus.Published)
        {
            await cacheService.SetAsync(ArticleCacheKeys.ArticleSlug(article.Slug.ToLowerInvariant()), detailDto, CacheTtl, cancellationToken);
            await cacheService.SetAsync(ArticleCacheKeys.ArticleDetail(article.Id), detailDto, CacheTtl, cancellationToken);
        }

        // 5. Ghi nhận tương tác: view buffer & reading history
        await RecordInteractionsAsync(article.Id, cancellationToken);

        return detailDto;
    }

    private async Task RecordInteractionsAsync(Guid articleId, CancellationToken cancellationToken)
    {
        // Ghi nhận view buffer qua Redis
        await sender.Send(new RecordArticleViewCommand(articleId), cancellationToken);

        // Ghi nhận lịch sử đọc nếu người dùng đã đăng nhập
        if (currentUser.IsAuthenticated)
        {
            try
            {
                var userId = currentUser.Id!.Value;
                var existingHistory = await dbContext.ReadingHistories
                    .FirstOrDefaultAsync(h => h.UserId == userId && h.ArticleId == articleId, cancellationToken);

                if (existingHistory is not null)
                {
                    existingHistory.ReadAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    dbContext.ReadingHistories.Add(new ReadingHistory
                    {
                        UserId = userId,
                        ArticleId = articleId,
                        ReadAt = DateTimeOffset.UtcNow
                    });
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi lưu lịch sử đọc cho UserId: {UserId}, ArticleId: {ArticleId}", currentUser.Id, articleId);
            }
        }
    }
}
