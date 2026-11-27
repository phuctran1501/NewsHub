namespace NewsHub.Application.Features.Companies.Queries.GetPublicCompanies;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Enums;

public sealed record GetPublicCompaniesQuery(
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 6) : IRequest<PagedList<PublicCompanyDto>>;

public sealed class GetPublicCompaniesQueryValidator : AbstractValidator<GetPublicCompaniesQuery>
{
    public GetPublicCompaniesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang phải từ 1 đến 50.");

        When(x => !string.IsNullOrEmpty(x.SearchTerm), () =>
        {
            RuleFor(x => x.SearchTerm!)
                .MaximumLength(100).WithMessage("Từ khóa tìm kiếm không được vượt quá 100 ký tự.");
        });
    }
}

public sealed record CachedPublicCompaniesPage(
    IReadOnlyList<PublicCompanyDto> Items,
    int TotalCount);

public sealed class GetPublicCompaniesQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<GetPublicCompaniesQuery, PagedList<PublicCompanyDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<PagedList<PublicCompanyDto>> Handle(
        GetPublicCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var cacheKey = $"companies:public:p{request.PageNumber}:s{request.PageSize}";
            var cached = await cacheService.GetOrCreateAsync(
                cacheKey,
                ct => FetchPageAsync(null, request.PageNumber, request.PageSize, ct),
                CacheTtl,
                cancellationToken);

            return new PagedList<PublicCompanyDto>(
                cached.Items,
                request.PageNumber,
                request.PageSize,
                cached.TotalCount);
        }

        var page = await FetchPageAsync(
            request.SearchTerm,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return new PagedList<PublicCompanyDto>(
            page.Items,
            request.PageNumber,
            request.PageSize,
            page.TotalCount);
    }

    private async Task<CachedPublicCompaniesPage> FetchPageAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Status == CompanyStatus.Approved);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmed = searchTerm.Trim();
            var lower = trimmed.ToLower();
            query = query.Where(c => c.Code == trimmed || c.Name.ToLower().Contains(lower));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.Articles.Count(a => a.Status == ArticleStatus.Published))
            .ThenByDescending(c => c.Followers.Count)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new PublicCompanyDto(
                c.Id,
                c.Name,
                c.Code,
                c.Owner != null ? c.Owner.FullName : null,
                c.Members.Count,
                c.Articles.Count(a => a.Status == ArticleStatus.Published),
                c.Followers.Count,
                c.CreatedAt))
            .ToListAsync(cancellationToken);

        return new CachedPublicCompaniesPage(items, totalCount);
    }
}
