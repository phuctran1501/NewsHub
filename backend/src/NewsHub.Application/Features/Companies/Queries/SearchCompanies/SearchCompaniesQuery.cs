namespace NewsHub.Application.Features.Companies.Queries.SearchCompanies;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record SearchCompaniesQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<CompanySummaryDto>>;

public sealed class SearchCompaniesQueryValidator : AbstractValidator<SearchCompaniesQuery>
{
    public SearchCompaniesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}

public sealed class SearchCompaniesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<SearchCompaniesQuery, PagedList<CompanySummaryDto>>
{
    public async Task<PagedList<CompanySummaryDto>> Handle(SearchCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền tìm kiếm tòa soạn.", ErrorCodes.PermissionDenied);
        }

        var query = dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Status == CompanyStatus.Approved);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(c => c.Code == request.SearchTerm.Trim() || c.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var companies = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CompanySummaryDto(
                c.Id,
                c.Name,
                c.Code,
                c.TaxCode,
                c.Owner != null ? c.Owner.FullName : null,
                c.Members.Count,
                c.Articles.Count(a => a.Status == ArticleStatus.Published),
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<CompanySummaryDto>(companies, request.PageNumber, request.PageSize, totalCount);
    }
}
