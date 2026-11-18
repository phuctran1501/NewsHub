namespace NewsHub.Application.Features.Articles.Queries.GetRelatedArticles;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetRelatedArticlesQuery(
    Guid Id,
    int Limit = 5) : IRequest<IReadOnlyList<ArticleSummaryDto>>;

public sealed class GetRelatedArticlesQueryValidator : AbstractValidator<GetRelatedArticlesQuery>
{
    public GetRelatedArticlesQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20).WithMessage("Số lượng bài gợi ý phải từ 1 đến 20.");
    }
}

public sealed class GetRelatedArticlesQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<GetRelatedArticlesQuery, IReadOnlyList<ArticleSummaryDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<ArticleSummaryDto>> Handle(
        GetRelatedArticlesQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = ArticleCacheKeys.ArticleRelated(request.Id);

        return await cacheService.GetOrCreateAsync<IReadOnlyList<ArticleSummaryDto>>(
            cacheKey,
            async ct =>
            {
                var targetArticle = await dbContext.Articles
                    .AsNoTracking()
                    .Include(a => a.ArticleCategories)
                    .FirstOrDefaultAsync(a => a.Id == request.Id, ct);

                if (targetArticle is null)
                {
                    throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
                }

                var categoryIds = targetArticle.ArticleCategories.Select(ac => ac.CategoryId).ToList();

                // Gợi ý bài viết liên quan: Ưu tiên các bài có chung chuyên mục, đã xuất bản, loại trừ bài viết hiện tại
                var relatedQuery = dbContext.Articles
                    .AsNoTracking()
                    .Where(a => a.Id != request.Id && a.Status == ArticleStatus.Published);

                if (categoryIds.Count > 0)
                {
                    relatedQuery = relatedQuery.Where(a => a.ArticleCategories.Any(ac => categoryIds.Contains(ac.CategoryId)));
                }

                var articles = await relatedQuery
                    .OrderByDescending(a => a.PublishedAt)
                    .ThenByDescending(a => a.Id)
                    .Take(request.Limit)
                    .Include(a => a.Author)
                    .Include(a => a.CoverImage)
                    .Include(a => a.ArticleCategories)
                        .ThenInclude(ac => ac.Category)
                    .ToListAsync(ct);

                return articles.Select(a =>
                {
                    var author = a.Author!;
                    var cover = a.CoverImage;
                    var categories = a.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();

                    return new ArticleSummaryDto(
                        a.Id,
                        a.Title,
                        a.Slug,
                        ExtractExcerpt(a.ContentHtml),
                        a.Status.ToString(),
                        new AuthorSummaryDto(author.Id, author.FullName, author.AvatarUrl),
                        a.CompanyId,
                        cover is null ? null : new CoverImageDto(cover.Id, cover.Url, cover.Width, cover.Height, cover.Format),
                        a.ReadingTimeMinutes,
                        a.ViewCount,
                        categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
                        a.PublishedAt,
                        a.CreatedAt);
                }).ToList();
            },
            CacheTtl,
            cancellationToken);
    }

    private static string? ExtractExcerpt(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ").Trim();
        if (plainText.Length <= 160)
        {
            return plainText;
        }

        return plainText[..160].Trim() + "...";
    }
}
