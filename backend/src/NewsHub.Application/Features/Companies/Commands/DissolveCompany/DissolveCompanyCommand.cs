namespace NewsHub.Application.Features.Companies.Commands.DissolveCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record DissolveCompanyCommand(Guid CompanyId) : IRequest, ITransactionalRequest;

public sealed class DissolveCompanyCommandValidator : AbstractValidator<DissolveCompanyCommand>
{
    public DissolveCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class DissolveCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<DissolveCompanyCommandHandler> logger) : IRequestHandler<DissolveCompanyCommand>
{
    public async Task Handle(DissolveCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền thực hiện thao tác này.", ErrorCodes.PermissionDenied);
        }

        var currentUserId = currentUser.Id!.Value;

        var ownerMember = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (ownerMember == null)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền giải thể tòa soạn.", ErrorCodes.OnlyOwnerAllowed);
        }

        var totalMembers = await dbContext.CompanyMembers
            .CountAsync(m => m.CompanyId == request.CompanyId, cancellationToken);

        if (totalMembers >= 2)
        {
            throw new BadRequestException("Tòa soạn vẫn còn các thành viên khác. Vui lòng chuyển giao quyền sở hữu hoặc xóa các thành viên trước khi giải thể.", ErrorCodes.CannotDissolveWithMembers);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        // Xóa thành viên của Owner để giải phóng slot công ty
        dbContext.CompanyMembers.Remove(ownerMember);

        // Chuyển trạng thái công ty sang Rejected (Giải thể) và lưu vết
        company.Status = CompanyStatus.Rejected;
        company.UpdatedAt = DateTimeOffset.UtcNow;

        // Hủy toàn bộ các yêu cầu/lời mời đang Pending của công ty để tránh việc phóng viên chấp nhận vào công ty đã giải thể
        await dbContext.CompanyRequests
            .Where(r => r.CompanyId == request.CompanyId && r.Status == CompanyRequestStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, CompanyRequestStatus.Rejected)
                .SetProperty(r => r.RespondedAt, DateTimeOffset.UtcNow), cancellationToken);

        // Giải phóng các bài viết nháp/chờ duyệt của Owner về cá nhân
        await dbContext.Articles
            .Where(a => a.AuthorId == currentUserId && a.CompanyId == request.CompanyId &&
                        (a.Status == ArticleStatus.Draft || a.Status == ArticleStatus.PendingCompanyReview))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.CompanyId, (Guid?)null)
                .SetProperty(a => a.Status, ArticleStatus.Draft)
                .SetProperty(a => a.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Owner {CurrentUserId} đã giải thể công ty {CompanyId}.", currentUserId, request.CompanyId);
    }
}
