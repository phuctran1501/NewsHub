namespace NewsHub.Application.Features.Articles.Queries.GetMyArticles;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Helpers;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetMyArticlesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    ArticleStatus? Status = null) : IRequest<PagedList<ArticleSummaryDto>>;

public sealed class GetMyArticlesQueryValidator : AbstractValidator<GetMyArticlesQuery>
{
    public GetMyArticlesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetMyArticlesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyArticlesQuery, PagedList<ArticleSummaryDto>>
{
    public async Task<PagedList<ArticleSummaryDto>> Handle(
        GetMyArticlesQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem danh sách bài viết cá nhân.", ErrorCodes.Unauthorized);
        }

        var authorId = currentUser.Id!.Value;
        var query = dbContext.Articles
            .AsNoTracking()
            .Where(a => a.AuthorId == authorId);

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue)
        {
            return new PagedList<ArticleSummaryDto>(new List<ArticleSummaryDto>(), request.PageNumber, request.PageSize, totalCount);
        }

        var articles = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
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
                HtmlExcerptHelper.ExtractExcerpt(a.ContentHtml),
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
}
