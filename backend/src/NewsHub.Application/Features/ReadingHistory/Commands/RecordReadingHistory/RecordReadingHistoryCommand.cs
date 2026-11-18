namespace NewsHub.Application.Features.ReadingHistory.Commands.RecordReadingHistory;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Entities;

public sealed record RecordReadingHistoryCommand(Guid ArticleId) : IRequest;

public sealed class RecordReadingHistoryCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<RecordReadingHistoryCommandHandler> logger) : IRequestHandler<RecordReadingHistoryCommand>
{
    public async Task Handle(RecordReadingHistoryCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return;
        }

        var userId = currentUser.Id!.Value;

        try
        {
            var existingHistory = await dbContext.ReadingHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.ArticleId == request.ArticleId, cancellationToken);

            if (existingHistory is not null)
            {
                existingHistory.ReadAt = DateTimeOffset.UtcNow;
            }
            else
            {
                dbContext.ReadingHistories.Add(new ReadingHistory
                {
                    UserId = userId,
                    ArticleId = request.ArticleId,
                    ReadAt = DateTimeOffset.UtcNow
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Ghi nhận lịch sử là tác vụ phụ, không làm gián đoạn luồng đọc bài của độc giả
            logger.LogWarning(ex, "Lỗi khi ghi nhận lịch sử đọc bài viết {ArticleId} cho user {UserId}",
                request.ArticleId, userId);
        }
    }
}
