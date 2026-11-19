namespace NewsHub.Application.Features.Companies.Queries.GetFollowedCompanies;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetFollowedCompaniesQuery(
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<FollowedCompanyDto>>;

public sealed class GetFollowedCompaniesQueryValidator : AbstractValidator<GetFollowedCompaniesQuery>
{
    public GetFollowedCompaniesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}

public sealed class GetFollowedCompaniesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetFollowedCompaniesQuery, PagedList<FollowedCompanyDto>>
{
    public async Task<PagedList<FollowedCompanyDto>> Handle(GetFollowedCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        var currentUserId = currentUser.Id!.Value;

        var query = dbContext.CompanyFollowers
            .AsNoTracking()
            .Where(f => f.UserId == currentUserId && f.Company != null && f.Company.Status == CompanyStatus.Approved);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new FollowedCompanyDto(
                f.CompanyId,
                f.Company != null ? f.Company.Name : string.Empty,
                f.Company != null ? f.Company.Code : string.Empty,
                f.Company != null && f.Company.Owner != null ? f.Company.Owner.FullName : null,
                f.Company != null ? f.Company.Members.Count : 0,
                f.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<FollowedCompanyDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
