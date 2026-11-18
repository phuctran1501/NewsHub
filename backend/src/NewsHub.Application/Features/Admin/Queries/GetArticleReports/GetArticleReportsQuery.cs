namespace NewsHub.Application.Features.Admin.Queries.GetArticleReports;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

/// <summary>
/// Query duyệt danh sách báo cáo vi phạm bài viết có phân trang và bộ lọc theo trạng thái, bài viết.
/// </summary>
public sealed record GetArticleReportsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    ReportStatus? Status = null,
    Guid? ArticleId = null) : IRequest<PagedList<AdminArticleReportDto>>;

public sealed class GetArticleReportsQueryValidator : AbstractValidator<GetArticleReportsQuery>
{
    public GetArticleReportsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetArticleReportsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetArticleReportsQuery, PagedList<AdminArticleReportDto>>
{
    public async Task<PagedList<AdminArticleReportDto>> Handle(
        GetArticleReportsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền xem danh sách báo cáo vi phạm.", ErrorCodes.PermissionDenied);
        }

        var query = dbContext.ArticleReports
            .AsNoTracking()
            .Include(r => r.Article)
            .Include(r => r.Reporter)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.ArticleId.HasValue)
        {
            query = query.Where(r => r.ArticleId == request.ArticleId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AdminArticleReportDto(
                r.Id,
                r.ArticleId,
                r.Article != null ? r.Article.Title : string.Empty,
                r.Article != null ? r.Article.Slug : string.Empty,
                r.Reporter != null
                    ? new AdminReportUserSummaryDto(r.Reporter.Id, r.Reporter.FullName, r.Reporter.Email.Value)
                    : null,
                r.Reason,
                r.Details,
                r.Status,
                r.CreatedAt,
                r.ResolvedAt,
                r.ResolvedBy))
            .ToListAsync(cancellationToken);

        return new PagedList<AdminArticleReportDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
