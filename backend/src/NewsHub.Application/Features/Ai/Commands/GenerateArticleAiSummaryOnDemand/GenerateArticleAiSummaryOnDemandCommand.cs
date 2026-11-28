namespace NewsHub.Application.Features.Ai.Commands.GenerateArticleAiSummaryOnDemand;

using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GenerateArticleAiSummaryOnDemandCommand(Guid ArticleId) : IRequest<ArticleAiSummaryDto>;

public sealed class GenerateArticleAiSummaryOnDemandCommandValidator
    : AbstractValidator<GenerateArticleAiSummaryOnDemandCommand>
{
    public GenerateArticleAiSummaryOnDemandCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed partial class GenerateArticleAiSummaryOnDemandCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IAiService aiService) : IRequestHandler<GenerateArticleAiSummaryOnDemandCommand, ArticleAiSummaryDto>
{
    public async Task<ArticleAiSummaryDto> Handle(
        GenerateArticleAiSummaryOnDemandCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException(
                "Vui lòng đăng nhập để sử dụng tính năng tóm tắt bài viết bằng AI.",
                ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (article is null || article.Status != ArticleStatus.Published)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var existing = await dbContext.AiSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ArticleId == request.ArticleId, cancellationToken);

        if (existing is not null)
        {
            return MapToDto(existing);
        }

        var plainText = StripHtmlTags(article.ContentHtml);
        var generated = await aiService.GenerateSummaryAndSentimentAsync(
            article.Title,
            plainText,
            cancellationToken);

        var sentiment = generated.Sentiment switch
        {
            "Positive" => ArticleSentiment.Positive,
            "Negative" => ArticleSentiment.Negative,
            "Mixed" => ArticleSentiment.Mixed,
            _ => ArticleSentiment.Neutral
        };

        var entity = new AiSummary
        {
            Id = Guid.CreateVersion7(),
            ArticleId = article.Id,
            BulletPoints = generated.BulletPoints.ToList(),
            Sentiment = sentiment,
            ExtractedTags = generated.ExtractedTags.ToList(),
            ConfidenceScore = generated.ConfidenceScore,
            AiModel = generated.AiModel,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        try
        {
            dbContext.AiSummaries.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return MapToDto(entity);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Xử lý race condition nếu hai độc giả bấm tóm tắt cùng lúc
            var persisted = await dbContext.AiSummaries
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ArticleId == request.ArticleId, cancellationToken);

            return persisted is not null ? MapToDto(persisted) : MapToDto(entity);
        }
    }

    private static ArticleAiSummaryDto MapToDto(AiSummary summary) =>
        new(
            Status: "Completed",
            BulletPoints: summary.BulletPoints,
            Sentiment: summary.Sentiment.ToString(),
            ExtractedTags: summary.ExtractedTags,
            ConfidenceScore: summary.ConfidenceScore,
            ProcessedAt: summary.ProcessedAt);

    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var withoutTags = HtmlTagRegex().Replace(html, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
