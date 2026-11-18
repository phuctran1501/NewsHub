namespace NewsHub.Application.Features.Articles.Commands.CreateArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Helpers;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Common;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record CreateArticleCommand(
    string Title,
    string ContentHtml,
    Guid? CoverImageId = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    Guid? CompanyId = null,
    string? SourceName = null,
    string? SourceUrl = null) : IRequest<ArticleDetailDto>;

public sealed class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề bài viết không được để trống.")
            .Must(t => !string.IsNullOrWhiteSpace(t) && t.Trim().Length >= 5 && t.Trim().Length <= 300)
            .WithMessage("Tiêu đề bài viết phải từ 5 đến 300 ký tự.");

        RuleFor(x => x.ContentHtml)
            .NotEmpty().WithMessage("Nội dung bài viết không được để trống.")
            .Must(c => !string.IsNullOrWhiteSpace(c) && c.Trim().Length >= 20)
            .WithMessage("Nội dung bài viết phải chứa ít nhất 20 ký tự.");

        When(x => x.CategoryIds != null, () =>
        {
            RuleFor(x => x.CategoryIds!)
                .Must(ids => ids.Count <= 5)
                .WithMessage("Một bài viết chỉ được gắn tối đa 5 chuyên mục.");
        });

        When(x => !string.IsNullOrEmpty(x.SourceName), () =>
        {
            RuleFor(x => x.SourceName!)
                .MaximumLength(100).WithMessage("Tên nguồn tin không được vượt quá 100 ký tự.");
        });

        When(x => !string.IsNullOrEmpty(x.SourceUrl), () =>
        {
            RuleFor(x => x.SourceUrl!)
                .MaximumLength(500).WithMessage("Đường dẫn nguồn tin không được vượt quá 500 ký tự.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Đường dẫn nguồn tin phải là URL hợp lệ (http/https).");
        });
    }
}

public sealed class CreateArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IHtmlSanitizer htmlSanitizer,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateArticleCommand, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(
        CreateArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tạo bài viết.", ErrorCodes.Unauthorized);
        }

        var authorId = currentUser.Id!.Value;
        var author = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == authorId, cancellationToken);

        if (author is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy thông tin tác giả.", ErrorCodes.UserNotFound);
        }

        // 1. Sanitize HTML
        var sanitizedHtml = htmlSanitizer.Sanitize(request.ContentHtml);
        if (string.IsNullOrWhiteSpace(sanitizedHtml))
        {
            throw new BadRequestException("Nội dung bài viết không hợp lệ sau khi làm sạch.", ErrorCodes.InvalidRequest);
        }

        // 2. Tự động tính thời gian đọc (200 wpm)
        var readingTime = ReadingTimeCalculator.Calculate(sanitizedHtml);

        // 3. Sinh Unique Slug (tự động phân biệt hậu tố nếu trùng)
        var baseSlug = SlugHelper.GenerateSlug(request.Title.Trim(), 300);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = $"bai-viet-{Guid.NewGuid():N}"[..20];
        }

        var uniqueSlug = await GenerateUniqueSlugAsync(baseSlug, cancellationToken);

        // 4. Kiểm tra ảnh bìa nếu có
        MediaFile? coverImage = null;
        if (request.CoverImageId.HasValue)
        {
            coverImage = await dbContext.MediaFiles
                .FirstOrDefaultAsync(m => m.Id == request.CoverImageId.Value, cancellationToken);

            if (coverImage is null)
            {
                throw NotFoundException.WithErrorCode("Ảnh bìa không tồn tại trong hệ thống.", ErrorCodes.MediaNotFound);
            }
        }

        // 5. Kiểm tra danh mục hợp lệ
        var categories = new List<Category>();
        if (request.CategoryIds != null && request.CategoryIds.Count > 0)
        {
            var distinctIds = request.CategoryIds.Distinct().ToList();
            categories = await dbContext.Categories
                .Where(c => distinctIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            if (categories.Count != distinctIds.Count)
            {
                throw NotFoundException.WithErrorCode("Một hoặc nhiều chuyên mục đã chọn không tồn tại.", ErrorCodes.CategoryNotFound);
            }
        }

        var article = new Article
        {
            AuthorId = authorId,
            CompanyId = request.CompanyId,
            Title = request.Title.Trim(),
            Slug = uniqueSlug,
            ContentHtml = sanitizedHtml,
            Status = ArticleStatus.Draft,
            SourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim(),
            SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            CoverImageId = request.CoverImageId,
            CoverImage = coverImage,
            ReadingTimeMinutes = readingTime,
            ViewCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var category in categories)
        {
            article.ArticleCategories.Add(new ArticleCategory
            {
                ArticleId = article.Id,
                CategoryId = category.Id,
                IsAiAssigned = false,
                Category = category
            });
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                dbContext.Articles.Add(article);
                await dbContext.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("Đường dẫn (slug) của bài viết đã tồn tại.", ErrorCodes.ArticleSlugConflict);
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            throw NotFoundException.WithErrorCode("Một hoặc nhiều chuyên mục hoặc ảnh bìa không tồn tại trong hệ thống.", ErrorCodes.CategoryNotFound);
        }

        return MapToDetailDto(article, author, coverImage, categories);
    }

    private async Task<string> GenerateUniqueSlugAsync(string baseSlug, CancellationToken cancellationToken)
    {
        var slugPattern = baseSlug + "%";
        var existingSlugs = await dbContext.Articles
            .AsNoTracking()
            .Where(a => a.Slug == baseSlug || EF.Functions.Like(a.Slug, slugPattern))
            .Select(a => a.Slug)
            .ToListAsync(cancellationToken);

        if (!existingSlugs.Contains(baseSlug))
        {
            return baseSlug;
        }

        var maxSuffix = 1;
        foreach (var slug in existingSlugs)
        {
            if (slug.StartsWith(baseSlug + "-"))
            {
                var suffixStr = slug[(baseSlug.Length + 1)..];
                if (int.TryParse(suffixStr, out var suffix) && suffix >= maxSuffix)
                {
                    maxSuffix = suffix + 1;
                }
            }
        }

        return $"{baseSlug}-{maxSuffix}";
    }

    private static ArticleDetailDto MapToDetailDto(
        Article article,
        User author,
        MediaFile? coverImage,
        IEnumerable<Category> categories)
    {
        var authorDto = new AuthorSummaryDto(author.Id, author.FullName, author.AvatarUrl);
        var coverDto = coverImage is null ? null : new CoverImageDto(
            coverImage.Id,
            coverImage.Url,
            coverImage.Width,
            coverImage.Height,
            coverImage.Format);

        var categoryDtos = categories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList();

        return new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.ContentHtml,
            article.Status.ToString(),
            authorDto,
            article.CompanyId,
            article.SourceName,
            article.SourceUrl,
            coverDto,
            article.ReadingTimeMinutes,
            article.ViewCount,
            categoryDtos,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt);
    }
}
