namespace NewsHub.Application.Features.Auth.Queries.GetCurrentUserProfile;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetCurrentUserProfileQuery : IRequest<UserProfileResponseDto>;

public sealed class GetCurrentUserProfileQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetCurrentUserProfileQuery, UserProfileResponseDto>
{
    public async Task<UserProfileResponseDto> Handle(GetCurrentUserProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Id == null)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện thao tác này.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw NotFoundException.WithErrorCode("Người dùng không tồn tại hoặc đã bị khóa.", ErrorCodes.UserNotFound);
        }

        return new UserProfileResponseDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.PhoneNumber,
            user.PressCardNumber,
            user.Bio,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
