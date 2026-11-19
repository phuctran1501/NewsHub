namespace NewsHub.Application.Features.Companies.Queries.GetCompanyJoinRequests;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetCompanyJoinRequestsQuery(Guid CompanyId) : IRequest<IReadOnlyList<CompanyRequestDto>>;

public sealed class GetCompanyJoinRequestsQueryValidator : AbstractValidator<GetCompanyJoinRequestsQuery>
{
    public GetCompanyJoinRequestsQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class GetCompanyJoinRequestsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetCompanyJoinRequestsQuery, IReadOnlyList<CompanyRequestDto>>
{
    public async Task<IReadOnlyList<CompanyRequestDto>> Handle(GetCompanyJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền thực hiện thao tác này.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var isOwner = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == userId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền xem danh sách đơn xin gia nhập.", ErrorCodes.OnlyOwnerAllowed);
        }

        var joinRequests = await dbContext.CompanyRequests
            .AsNoTracking()
            .Where(r => r.CompanyId == request.CompanyId && r.Type == CompanyRequestType.JoinRequest && r.Status == CompanyRequestStatus.Pending)
            .Include(r => r.Journalist)
            .Include(r => r.Company)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CompanyRequestDto(
                r.Id,
                r.CompanyId,
                r.Company != null ? r.Company.Name : string.Empty,
                r.Company != null ? r.Company.Code : string.Empty,
                r.JournalistId,
                r.Journalist != null ? r.Journalist.FullName : string.Empty,
                r.Journalist != null ? ((string)r.Journalist.Email) : string.Empty,
                r.Journalist != null ? r.Journalist.PressCardNumber : null,
                r.Type,
                r.Status,
                r.CreatedAt,
                r.RespondedAt
            ))
            .ToListAsync(cancellationToken);

        return joinRequests;
    }
}
