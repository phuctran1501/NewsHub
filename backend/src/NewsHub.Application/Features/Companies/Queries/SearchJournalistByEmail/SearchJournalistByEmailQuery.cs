namespace NewsHub.Application.Features.Companies.Queries.SearchJournalistByEmail;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record SearchJournalistByEmailQuery(Guid CompanyId, string Email) : IRequest<JournalistSearchResultDto>;

public sealed class SearchJournalistByEmailQueryValidator : AbstractValidator<SearchJournalistByEmailQuery>
{
    public SearchJournalistByEmailQueryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email nhà báo không được để trống.")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ.");
    }
}

public sealed class SearchJournalistByEmailQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<SearchJournalistByEmailQuery, JournalistSearchResultDto>
{
    public async Task<JournalistSearchResultDto> Handle(SearchJournalistByEmailQuery request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền tìm kiếm nhà báo để mời.", ErrorCodes.OnlyOwnerAllowed);
        }

        var normalizedEmail = request.Email.Trim().ToLower();

        var targetUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => ((string)u.Email) == normalizedEmail, cancellationToken);

        if (targetUser == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy người dùng với địa chỉ email này.", ErrorCodes.UserNotFound);
        }

        if (targetUser.Role != UserRole.Journalist)
        {
            throw new BadRequestException("Tài khoản này là Độc giả thông thường, không thể mời vào Tòa soạn.", ErrorCodes.UserIsNotJournalist);
        }

        var isAlreadyMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == targetUser.Id, cancellationToken);

        var activeCompaniesCount = await dbContext.CompanyMembers
            .CountAsync(m => m.UserId == targetUser.Id, cancellationToken);

        var canInvite = !isAlreadyMember && activeCompaniesCount < 5;

        return new JournalistSearchResultDto(
            targetUser.Id,
            targetUser.FullName,
            (string)targetUser.Email,
            targetUser.AvatarUrl,
            targetUser.PressCardNumber,
            activeCompaniesCount,
            isAlreadyMember,
            canInvite
        );
    }
}
