namespace NewsHub.Application.Features.Companies.Queries.GetCompanyMembers;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetCompanyMembersQuery(Guid CompanyId) : IRequest<IReadOnlyList<CompanyMemberDto>>;

public sealed class GetCompanyMembersQueryValidator : AbstractValidator<GetCompanyMembersQuery>
{
    public GetCompanyMembersQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class GetCompanyMembersQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetCompanyMembersQuery, IReadOnlyList<CompanyMemberDto>>
{
    public async Task<IReadOnlyList<CompanyMemberDto>> Handle(GetCompanyMembersQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền truy cập danh sách nhân viên.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var isOwner = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == userId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền xem danh sách nhân viên.", ErrorCodes.OnlyOwnerAllowed);
        }

        var members = await dbContext.CompanyMembers
            .AsNoTracking()
            .Where(m => m.CompanyId == request.CompanyId)
            .Include(m => m.User)
            .OrderBy(m => m.Role == CompanyMemberRole.Owner ? 0 : 1)
            .ThenBy(m => m.JoinedAt)
            .Select(m => new CompanyMemberDto(
                m.UserId,
                m.User != null ? m.User.FullName : string.Empty,
                m.User != null ? ((string)m.User.Email) : string.Empty,
                m.User != null ? m.User.AvatarUrl : null,
                m.User != null ? m.User.PressCardNumber : null,
                m.Role,
                m.JoinedAt,
                dbContext.Articles.Count(a => a.CompanyId == request.CompanyId && a.AuthorId == m.UserId && a.Status == ArticleStatus.Published)
            ))
            .ToListAsync(cancellationToken);

        return members;
    }
}
