namespace NewsHub.Application.Features.Companies.Queries.GetMyCompanies;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetMyCompaniesQuery : IRequest<IReadOnlyList<MyCompanyDto>>;

public sealed class GetMyCompaniesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyCompaniesQuery, IReadOnlyList<MyCompanyDto>>
{
    public async Task<IReadOnlyList<MyCompanyDto>> Handle(GetMyCompaniesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có danh sách tòa soạn tham gia.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var myCompanies = await dbContext.CompanyMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Include(m => m.Company)
            .OrderByDescending(m => m.JoinedAt)
            .Select(m => new MyCompanyDto(
                m.CompanyId,
                m.Company != null ? m.Company.Name : string.Empty,
                m.Company != null ? m.Company.Code : string.Empty,
                m.Company != null ? m.Company.TaxCode : string.Empty,
                m.Role,
                m.JoinedAt
            ))
            .ToListAsync(cancellationToken);

        return myCompanies;
    }
}
