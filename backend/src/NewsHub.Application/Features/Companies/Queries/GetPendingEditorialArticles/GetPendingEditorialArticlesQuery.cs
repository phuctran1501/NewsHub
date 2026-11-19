namespace NewsHub.Application.Features.Companies.Queries.GetPendingEditorialArticles;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetPendingEditorialArticlesQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<EditorialArticleDto>>;

public sealed class GetPendingEditorialArticlesQueryValidator : AbstractValidator<GetPendingEditorialArticlesQuery>
{
    public GetPendingEditorialArticlesQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}

public sealed class GetPendingEditorialArticlesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetPendingEditorialArticlesQuery, PagedList<EditorialArticleDto>>
{
    public async Task<PagedList<EditorialArticleDto>> Handle(GetPendingEditorialArticlesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền thực hiện thao tác này.", ErrorCodes.PermissionDenied);
        }

        var currentUserId = currentUser.Id!.Value;

        var isOwner = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền xem danh sách bài viết chờ duyệt.", ErrorCodes.OnlyOwnerAllowed);
        }

        var query = dbContext.Articles
            .AsNoTracking()
            .Where(a => a.CompanyId == request.CompanyId && a.Status == ArticleStatus.PendingCompanyReview);

        var totalCount = await query.CountAsync(cancellationToken);

        var articles = await query
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new EditorialArticleDto(
                a.Id,
                a.Title,
                a.Slug,
                a.AuthorId,
                a.Author != null ? a.Author.FullName : string.Empty,
                a.Author != null ? a.Author.AvatarUrl : null,
                a.Status,
                a.ReadingTimeMinutes,
                a.CreatedAt,
                a.UpdatedAt,
                a.PublishedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<EditorialArticleDto>(articles, request.PageNumber, request.PageSize, totalCount);
    }
}
