namespace NewsHub.Application.Features.ReadingHistory.Queries.GetMyReadingHistory;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.ReadingHistory.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetMyReadingHistoryQuery(
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<ReadingHistoryItemDto>>;

public sealed class GetMyReadingHistoryQueryValidator : AbstractValidator<GetMyReadingHistoryQuery>
{
    public GetMyReadingHistoryQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetMyReadingHistoryQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyReadingHistoryQuery, PagedList<ReadingHistoryItemDto>>
{
    public async Task<PagedList<ReadingHistoryItemDto>> Handle(
        GetMyReadingHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem lịch sử đọc.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        // Chỉ hiển thị lịch sử của các bài viết đang ở trạng thái Published
        var query = dbContext.ReadingHistories
            .AsNoTracking()
            .Where(rh => rh.UserId == userId && rh.Article != null && rh.Article.Status == ArticleStatus.Published);

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.PageNumber - 1) * request.PageSize;
        if (offset >= totalCount)
        {
            return new PagedList<ReadingHistoryItemDto>([], request.PageNumber, request.PageSize, totalCount);
        }

        var histories = await query
            .OrderByDescending(rh => rh.ReadAt)
            .ThenByDescending(rh => rh.Id)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Include(rh => rh.Article)
                .ThenInclude(a => a!.CoverImage)
            .ToListAsync(cancellationToken);

        var items = histories.Select(rh =>
        {
            var article = rh.Article!;
            var cover = article.CoverImage;

            return new ReadingHistoryItemDto(
                rh.Id,
                article.Id,
                article.Title,
                article.Slug,
                cover?.Url,
                rh.ReadAt);
        }).ToList();

        return new PagedList<ReadingHistoryItemDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
