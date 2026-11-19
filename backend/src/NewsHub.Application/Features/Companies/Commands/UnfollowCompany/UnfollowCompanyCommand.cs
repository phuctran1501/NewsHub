namespace NewsHub.Application.Features.Companies.Commands.UnfollowCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record UnfollowCompanyCommand(Guid CompanyId) : IRequest;

public sealed class UnfollowCompanyCommandValidator : AbstractValidator<UnfollowCompanyCommand>
{
    public UnfollowCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class UnfollowCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<UnfollowCompanyCommandHandler> logger) : IRequestHandler<UnfollowCompanyCommand>
{
    public async Task Handle(UnfollowCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện thao tác này.", ErrorCodes.Unauthorized);
        }

        var currentUserId = currentUser.Id!.Value;

        var isMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId, cancellationToken);

        if (isMember)
        {
            throw new BadRequestException("Bạn đang là thành viên của tòa soạn này nên không thể thực hiện thao tác theo dõi.", ErrorCodes.CannotUnfollowOwnCompany);
        }

        var follower = await dbContext.CompanyFollowers
            .FirstOrDefaultAsync(f => f.CompanyId == request.CompanyId && f.UserId == currentUserId, cancellationToken);

        if (follower != null)
        {
            dbContext.CompanyFollowers.Remove(follower);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Người dùng {UserId} đã bỏ theo dõi tòa soạn {CompanyId}.", currentUserId, request.CompanyId);
        }
    }
}
