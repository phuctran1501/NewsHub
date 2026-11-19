namespace NewsHub.Application.Features.Ai.Queries.GetArticleAiSummary;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetArticleAiSummaryQuery(Guid ArticleId) : IRequest<ArticleAiSummaryDto>;

public sealed class GetArticleAiSummaryQueryValidator : AbstractValidator<GetArticleAiSummaryQuery>
{
    public GetArticleAiSummaryQueryValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class GetArticleAiSummaryQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetArticleAiSummaryQuery, ArticleAiSummaryDto>
{
    public async Task<ArticleAiSummaryDto> Handle(
        GetArticleAiSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var article = await dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (article == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var summary = await dbContext.AiSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ArticleId == request.ArticleId, cancellationToken);

        if (summary == null)
        {
            // Nếu bài viết đã xuất bản nhưng AI chưa tóm tắt xong, trả về trạng thái Processing
            if (article.Status == ArticleStatus.Published)
            {
                return new ArticleAiSummaryDto(Status: "Processing");
            }

            throw NotFoundException.WithErrorCode("Bản tóm tắt AI chưa sẵn sàng cho bài viết này.", ErrorCodes.AiSummaryNotFound);
        }

        return new ArticleAiSummaryDto(
            Status: "Completed",
            BulletPoints: summary.BulletPoints,
            Sentiment: summary.Sentiment.ToString(),
            ExtractedTags: summary.ExtractedTags,
            ConfidenceScore: summary.ConfidenceScore,
            ProcessedAt: summary.ProcessedAt);
    }
}
