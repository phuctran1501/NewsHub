namespace NewsHub.Application.Features.Articles.Commands.RecordArticleView;

using MediatR;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;

public sealed record RecordArticleViewCommand(Guid ArticleId) : IRequest<Unit>;

public sealed class RecordArticleViewCommandHandler(
    ICacheService cacheService,
    ILogger<RecordArticleViewCommandHandler> logger) : IRequestHandler<RecordArticleViewCommand, Unit>
{
    public async Task<Unit> Handle(
        RecordArticleViewCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var bufferKey = ArticleCacheKeys.ViewsBuffer(request.ArticleId);
            await cacheService.IncrementAsync(bufferKey, cancellationToken: cancellationToken);
            await cacheService.AddToSetAsync(ArticleCacheKeys.DirtyViewsSet, request.ArticleId.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi khi ghi nhận view đệm cho ArticleId: {ArticleId}", request.ArticleId);
        }

        return Unit.Value;
    }
}
