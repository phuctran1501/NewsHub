namespace NewsHub.Application.Features.Companies.Commands.FollowCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record FollowCompanyCommand(Guid CompanyId) : IRequest;

public sealed class FollowCompanyCommandValidator : AbstractValidator<FollowCompanyCommand>
{
    public FollowCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class FollowCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<FollowCompanyCommandHandler> logger) : IRequestHandler<FollowCompanyCommand>
{
    public async Task Handle(FollowCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để theo dõi tòa soạn.", ErrorCodes.Unauthorized);
        }

        var currentUserId = currentUser.Id!.Value;

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.Status == CompanyStatus.Approved, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn hoặc tòa soạn chưa được phê duyệt.", ErrorCodes.CompanyNotFound);
        }

        // Ràng buộc bất biến: Thành viên công ty tuyệt đối không được theo dõi chính công ty mình
        var isMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId, cancellationToken);

        if (isMember)
        {
            throw new BadRequestException("Bạn đang là thành viên của tòa soạn này nên không thể bấm theo dõi.", ErrorCodes.CannotFollowOwnCompany);
        }

        var isAlreadyFollowing = await dbContext.CompanyFollowers
            .AnyAsync(f => f.CompanyId == request.CompanyId && f.UserId == currentUserId, cancellationToken);

        if (isAlreadyFollowing)
        {
            // Idempotent
            return;
        }

        var follower = new CompanyFollower
        {
            CompanyId = request.CompanyId,
            UserId = currentUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.CompanyFollowers.Add(follower);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Người dùng {UserId} đã theo dõi tòa soạn {CompanyId}.", currentUserId, request.CompanyId);
        }
        catch (DbUpdateException)
        {
            // Idempotent: Nếu request đồng thời đã chèn bản ghi, bỏ qua lỗi duplicate key
        }
    }
}
