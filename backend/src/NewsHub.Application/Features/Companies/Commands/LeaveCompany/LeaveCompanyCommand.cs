namespace NewsHub.Application.Features.Companies.Commands.LeaveCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record LeaveCompanyCommand(Guid CompanyId) : IRequest, ITransactionalRequest;

public sealed class LeaveCompanyCommandValidator : AbstractValidator<LeaveCompanyCommand>
{
    public LeaveCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class LeaveCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<LeaveCompanyCommandHandler> logger) : IRequestHandler<LeaveCompanyCommand>
{
    public async Task Handle(LeaveCompanyCommand request, CancellationToken cancellationToken)
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

        var member = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == userId, cancellationToken);

        if (member == null)
        {
            throw NotFoundException.WithErrorCode("Bạn không phải là thành viên của tòa soạn này.", ErrorCodes.NotCompanyMember);
        }

        if (member.Role == CompanyMemberRole.Owner)
        {
            throw new BadRequestException("Chủ sở hữu không thể tự ý rời tòa soạn. Vui lòng chuyển giao quyền Owner cho thành viên khác trước khi rời.", ErrorCodes.CannotLeaveAsOwner);
        }

        dbContext.CompanyMembers.Remove(member);

        // Tự động giải phóng các bài viết nháp (Draft) và chờ duyệt (PendingCompanyReview) của phóng viên về cá nhân
        await dbContext.Articles
            .Where(a => a.AuthorId == userId && a.CompanyId == request.CompanyId &&
                        (a.Status == ArticleStatus.Draft || a.Status == ArticleStatus.PendingCompanyReview))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.CompanyId, (Guid?)null)
                .SetProperty(a => a.Status, ArticleStatus.Draft)
                .SetProperty(a => a.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Nhà báo {UserId} đã rời khỏi công ty {CompanyId}. Các bài nháp và chờ duyệt đã được chuyển về cá nhân.",
            userId, request.CompanyId);
    }
}
