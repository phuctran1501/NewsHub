namespace NewsHub.Application.Features.Articles.Queries.GetPublishedArticles;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Enums;

public sealed record GetPublishedArticlesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? CategorySlug = null,
    string? SearchTerm = null,
    Guid? AuthorId = null,
    Guid? CompanyId = null,
    string? SortBy = "latest") : IRequest<PagedList<ArticleSummaryDto>>;

public sealed class GetPublishedArticlesQueryValidator : AbstractValidator<GetPublishedArticlesQuery>
{
    public GetPublishedArticlesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");

        When(x => !string.IsNullOrEmpty(x.SearchTerm), () =>
        {
            RuleFor(x => x.SearchTerm!)
                .MaximumLength(100).WithMessage("Từ khóa tìm kiếm không được vượt quá 100 ký tự.");
        });
    }
}

public sealed class GetPublishedArticlesQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetPublishedArticlesQuery, PagedList<ArticleSummaryDto>>
{
    public async Task<PagedList<ArticleSummaryDto>> Handle(
        GetPublishedArticlesQuery request,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published);

        // 1. Bộ lọc chuyên mục
        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var cleanSlug = request.CategorySlug.Trim().ToLowerInvariant();
            query = query.Where(a => a.ArticleCategories.Any(ac => ac.Category != null && ac.Category.Slug == cleanSlug));
        }

        // 2. Bộ lọc tìm kiếm từ khóa trong tiêu đề
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(a => EF.Functions.Like(a.Title.ToLower(), $"%{term}%"));
        }

        // 3. Bộ lọc tác giả
        if (request.AuthorId.HasValue)
        {
            query = query.Where(a => a.AuthorId == request.AuthorId.Value);
        }

        // 4. Bộ lọc tòa soạn
        if (request.CompanyId.HasValue)
        {
            query = query.Where(a => a.CompanyId == request.CompanyId.Value);
        }

        // 5. Đếm tổng số bản ghi
        var totalCount = await query.CountAsync(cancellationToken);

        // 6. Sắp xếp kết quả có tie-breaker
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(a => a.PublishedAt).ThenBy(a => a.Id),
            "mostread" => query.OrderByDescending(a => a.ViewCount).ThenByDescending(a => a.Id),
            _ => query.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id)
        };

        // 7. Phân trang an toàn chống tràn số nguyên
        var offset = (long)(request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue)
        {
            return new PagedList<ArticleSummaryDto>(new List<ArticleSummaryDto>(), request.PageNumber, request.PageSize, totalCount);
        }

        var articles = await query
            .Skip((int)offset)
            .Take(request.PageSize)
            .Include(a => a.Author)
            .Include(a => a.CoverImage)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .ToListAsync(cancellationToken);

        var items = articles.Select(a =>
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

        return new PagedList<ArticleSummaryDto>(items, request.PageNumber, request.PageSize, totalCount);
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
