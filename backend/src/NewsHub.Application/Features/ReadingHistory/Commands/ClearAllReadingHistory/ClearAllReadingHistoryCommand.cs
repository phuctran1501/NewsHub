namespace NewsHub.Application.Features.ReadingHistory.Commands.ClearAllReadingHistory;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record ClearAllReadingHistoryCommand : IRequest<int>;

public sealed class ClearAllReadingHistoryCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<ClearAllReadingHistoryCommand, int>
{
    public async Task<int> Handle(ClearAllReadingHistoryCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xóa toàn bộ lịch sử đọc.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id!.Value;

        // Xóa set-based trực tiếp trên PostgreSQL với hiệu năng O(1) RAM
        var deletedCount = await dbContext.ReadingHistories
            .Where(rh => rh.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount;
    }
}
