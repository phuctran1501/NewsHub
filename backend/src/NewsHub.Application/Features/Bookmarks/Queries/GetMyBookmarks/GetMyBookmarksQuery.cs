namespace NewsHub.Application.Features.Bookmarks.Queries.GetMyBookmarks;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Bookmarks.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetMyBookmarksQuery(
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<BookmarkItemDto>>;

public sealed class GetMyBookmarksQueryValidator : AbstractValidator<GetMyBookmarksQuery>
{
    public GetMyBookmarksQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetMyBookmarksQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyBookmarksQuery, PagedList<BookmarkItemDto>>
{
    public async Task<PagedList<BookmarkItemDto>> Handle(
        GetMyBookmarksQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem danh sách bài viết đã lưu.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id!.Value;

        var query = dbContext.UserBookmarks
            .AsNoTracking()
            .Where(ub => ub.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.PageNumber - 1) * request.PageSize;

        var bookmarks = await query
            .OrderByDescending(ub => ub.SavedAt)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Include(ub => ub.Article)
                .ThenInclude(a => a!.Author)
            .Include(ub => ub.Article)
                .ThenInclude(a => a!.CoverImage)
            .Include(ub => ub.Article)
                .ThenInclude(a => a!.ArticleCategories)
                    .ThenInclude(ac => ac.Category)
            .ToListAsync(cancellationToken);

        var items = bookmarks.Select(ub =>
        {
            var article = ub.Article!;
            var author = article.Author;
            var cover = article.CoverImage;
            var primaryCategory = article.ArticleCategories
                .Select(ac => ac.Category)
                .FirstOrDefault(c => c != null);

            return new BookmarkItemDto(
                article.Id,
                article.Title,
                article.Slug,
                cover?.Url,
                ub.SavedAt,
                author?.FullName,
                primaryCategory?.Name);
        }).ToList();

        return new PagedList<BookmarkItemDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
