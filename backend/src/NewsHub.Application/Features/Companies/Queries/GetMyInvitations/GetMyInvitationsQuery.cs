namespace NewsHub.Application.Features.Companies.Queries.GetMyInvitations;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetMyInvitationsQuery : IRequest<IReadOnlyList<CompanyRequestDto>>;

public sealed class GetMyInvitationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyInvitationsQuery, IReadOnlyList<CompanyRequestDto>>
{
    public async Task<IReadOnlyList<CompanyRequestDto>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền xem danh sách lời mời.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var invitations = await dbContext.CompanyRequests
            .AsNoTracking()
            .Where(r => r.JournalistId == userId && r.Type == CompanyRequestType.Invitation && r.Status == CompanyRequestStatus.Pending)
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

        return invitations;
    }
}
