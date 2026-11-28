namespace NewsHub.Application.Features.Articles.Queries.GetDraftArticleByPreviewToken;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetDraftArticleByPreviewTokenQuery(string Token) : IRequest<ArticleDetailDto>;

public sealed class GetDraftArticleByPreviewTokenQueryValidator : AbstractValidator<GetDraftArticleByPreviewTokenQuery>
{
    public GetDraftArticleByPreviewTokenQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Preview token không được để trống.");
    }
}

public sealed class GetDraftArticleByPreviewTokenQueryHandler(
    IApplicationDbContext dbContext,
    IDraftPreviewTokenService draftPreviewTokenService) : IRequestHandler<GetDraftArticleByPreviewTokenQuery, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(
        GetDraftArticleByPreviewTokenQuery request,
        CancellationToken cancellationToken)
    {
        if (!draftPreviewTokenService.ValidatePreviewToken(request.Token, out var articleId))
        {
            throw new UnauthorizedException(
                "Liên kết xem thử bài viết không hợp lệ hoặc đã hết hạn hiệu lực (2 giờ).",
                ErrorCodes.InvalidPreviewToken);
        }

        var article = await dbContext.Articles
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Company)
            .Include(a => a.CoverImage)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var author = article.Author!;
        var cover = article.CoverImage;
        var categories = article.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();

        return new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.ContentHtml,
            article.Status.ToString(),
            new AuthorSummaryDto(author.Id, author.FullName, author.AvatarUrl),
            article.CompanyId,
            article.SourceName,
            article.SourceUrl,
            cover is null ? null : new CoverImageDto(cover.Id, cover.Url, cover.Width, cover.Height, cover.Format),
            article.ReadingTimeMinutes,
            article.ViewCount,
            categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            article.Company?.Name);
    }
}
