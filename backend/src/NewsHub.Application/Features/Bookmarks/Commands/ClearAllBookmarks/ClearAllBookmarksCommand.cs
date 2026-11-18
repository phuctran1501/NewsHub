namespace NewsHub.Application.Features.Bookmarks.Commands.ClearAllBookmarks;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record ClearAllBookmarksCommand : IRequest<int>;

public sealed class ClearAllBookmarksCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<ClearAllBookmarksCommand, int>
{
    public async Task<int> Handle(ClearAllBookmarksCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xóa toàn bộ bài viết đã lưu.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id!.Value;

        // Thực hiện xóa set-based trực tiếp trên PostgreSQL với hiệu năng O(1) RAM
        var deletedCount = await dbContext.UserBookmarks
            .Where(ub => ub.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount;
    }
}
