namespace NewsHub.Infrastructure.Jobs;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Pgvector;

/// <summary>
/// Triển khai các tác vụ nền AI xử lý qua Hangfire (Hàng đợi: ai-summarize).
/// </summary>
public class AiBackgroundJobs(
    IApplicationDbContext dbContext,
    IAiService aiService,
    INotificationService notificationService,
    ILogger<AiBackgroundJobs> logger) : IAiBackgroundJobs
{
    public async Task GenerateArticleSummaryAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bắt đầu tác vụ nền tạo tóm tắt AI cho ArticleId: {ArticleId}", articleId);

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null || article.Status != ArticleStatus.Published)
        {
            logger.LogWarning("Bài viết {ArticleId} không tồn tại hoặc chưa xuất bản. Bỏ qua tạo tóm tắt AI.", articleId);
            return;
        }

        try
        {
            var summaryResult = await aiService.GenerateSummaryAndSentimentAsync(
                article.Title,
                article.ContentHtml,
                cancellationToken);

            var sentiment = summaryResult.Sentiment switch
            {
                "Positive" => ArticleSentiment.Positive,
                "Negative" => ArticleSentiment.Negative,
                "Mixed" => ArticleSentiment.Mixed,
                _ => ArticleSentiment.Neutral
            };

            var existingSummary = await dbContext.AiSummaries
                .FirstOrDefaultAsync(s => s.ArticleId == articleId, cancellationToken);

            if (existingSummary != null)
            {
                existingSummary.BulletPoints = summaryResult.BulletPoints.ToList();
                existingSummary.Sentiment = sentiment;
                existingSummary.ExtractedTags = summaryResult.ExtractedTags.ToList();
                existingSummary.ConfidenceScore = summaryResult.ConfidenceScore;
                existingSummary.AiModel = summaryResult.AiModel;
                existingSummary.ProcessedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newSummary = new AiSummary
                {
                    Id = Guid.CreateVersion7(),
                    ArticleId = articleId,
                    BulletPoints = summaryResult.BulletPoints.ToList(),
                    Sentiment = sentiment,
                    ExtractedTags = summaryResult.ExtractedTags.ToList(),
                    ConfidenceScore = summaryResult.ConfidenceScore,
                    AiModel = summaryResult.AiModel,
                    ProcessedAt = DateTimeOffset.UtcNow
                };
                dbContext.AiSummaries.Add(newSummary);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã lưu bản tóm tắt AI thành công cho bài viết {ArticleId}", articleId);

            // 1. Đẩy sự kiện SignalR trực tiếp
            try
            {
                await notificationService.NotifyAiSummaryCompletedAsync(
                    article.AuthorId,
                    article.Id,
                    summaryResult.Sentiment,
                    summaryResult.BulletPoints.ToArray(),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi bắn sự kiện SignalR AiSummaryCompleted cho tác giả {AuthorId}", article.AuthorId);
            }

            // 2. Gửi thông báo lưu DB cá nhân qua SendToUserAsync
            try
            {
                var notification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.AiSummaryCompleted,
                    "Bản tóm tắt AI đã sẵn sàng",
                    $"Bài viết '{article.Title}' đã được AI tóm tắt xong với sắc thái {summaryResult.Sentiment}.",
                    article.Id,
                    $"/articles/{article.Slug}");

                await notificationService.SendToUserAsync(article.AuthorId, notification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi lưu thông báo AI cho tác giả {AuthorId}", article.AuthorId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi trong quá trình tạo tóm tắt AI cho bài viết {ArticleId}", articleId);
            throw; // Ném lại để Hangfire kích hoạt cơ chế retry tự động
        }
    }

    public async Task GenerateArticleEmbeddingAsync(Guid articleId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bắt đầu tác vụ nền sinh vector nhúng NLP cho ArticleId: {ArticleId}", articleId);

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article == null || article.Status != ArticleStatus.Published)
        {
            logger.LogWarning("Bài viết {ArticleId} không tồn tại hoặc chưa xuất bản. Bỏ qua sinh vector nhúng.", articleId);
            return;
        }

        try
        {
            var textToEmbed = $"{article.Title} {article.ContentHtml[..Math.Min(1000, article.ContentHtml.Length)]}";
            var embeddingArray = await aiService.GenerateEmbeddingAsync(textToEmbed, cancellationToken);

            var vector = new Vector(embeddingArray);

            var existingEmbedding = await dbContext.ArticleEmbeddings
                .FirstOrDefaultAsync(e => e.ArticleId == articleId, cancellationToken);

            if (existingEmbedding != null)
            {
                existingEmbedding.Embedding = vector;
                existingEmbedding.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                var newEmbedding = new ArticleEmbedding
                {
                    Id = Guid.CreateVersion7(),
                    ArticleId = articleId,
                    Embedding = vector,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                dbContext.ArticleEmbeddings.Add(newEmbedding);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã lưu vector nhúng pgvector thành công cho bài viết {ArticleId}", articleId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi trong quá trình sinh vector nhúng cho bài viết {ArticleId}", articleId);
            throw; // Ném lại để Hangfire kích hoạt retry
        }
    }
}
