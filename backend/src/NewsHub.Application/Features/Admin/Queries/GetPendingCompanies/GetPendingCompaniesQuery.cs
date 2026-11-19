namespace NewsHub.Application.Features.Admin.Queries.GetPendingCompanies;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetPendingCompaniesQuery(
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedList<PendingCompanyAdminDto>>;

public sealed class GetPendingCompaniesQueryValidator : AbstractValidator<GetPendingCompaniesQuery>
{
    public GetPendingCompaniesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang phải từ 1 đến 50.");
    }
}

public sealed class GetPendingCompaniesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetPendingCompaniesQuery, PagedList<PendingCompanyAdminDto>>
{
    public async Task<PagedList<PendingCompanyAdminDto>> Handle(GetPendingCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền xem danh sách tòa soạn chờ phê duyệt.", ErrorCodes.PermissionDenied);
        }

        var query = dbContext.Companies
            .AsNoTracking()
            .Where(c => c.Status == CompanyStatus.Pending);

        var totalCount = await query.CountAsync(cancellationToken);

        var companies = await query
            .OrderBy(c => c.CreatedAt) // First come, first served
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new PendingCompanyAdminDto(
                c.Id,
                c.Name,
                c.Code,
                c.TaxCode,
                c.OwnerId,
                c.Owner != null ? c.Owner.FullName : string.Empty,
                c.Owner != null ? ((string)c.Owner.Email) : string.Empty,
                c.Owner != null ? c.Owner.PressCardNumber : null,
                c.Status,
                c.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedList<PendingCompanyAdminDto>(companies, request.PageNumber, request.PageSize, totalCount);
    }
}
