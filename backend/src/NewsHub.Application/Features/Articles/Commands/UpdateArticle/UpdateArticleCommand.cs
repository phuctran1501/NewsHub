namespace NewsHub.Application.Features.Articles.Commands.UpdateArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Helpers;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Common;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record UpdateArticleCommand(
    Guid Id,
    string? Title = null,
    string? ContentHtml = null,
    Guid? CoverImageId = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    string? SourceName = null,
    string? SourceUrl = null) : IRequest<ArticleDetailDto>;

public sealed class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");

        When(x => !string.IsNullOrEmpty(x.Title), () =>
        {
            RuleFor(x => x.Title!)
                .Must(t => t.Trim().Length >= 5 && t.Trim().Length <= 300)
                .WithMessage("Tiêu đề bài viết phải từ 5 đến 300 ký tự.");
        });

        When(x => !string.IsNullOrEmpty(x.ContentHtml), () =>
        {
            RuleFor(x => x.ContentHtml!)
                .Must(c => c.Trim().Length >= 20)
                .WithMessage("Nội dung bài viết phải chứa ít nhất 20 ký tự.");
        });

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

public sealed class UpdateArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IHtmlSanitizer htmlSanitizer,
    ICacheService cacheService,
    IUnitOfWork unitOfWork,
    IBackgroundJobService? backgroundJobService = null,
    ILogger<UpdateArticleCommandHandler>? logger = null) : IRequestHandler<UpdateArticleCommand, ArticleDetailDto>
{
    public async Task<ArticleDetailDto> Handle(
        UpdateArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để cập nhật bài viết.", ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .Include(a => a.CoverImage)
            .Include(a => a.ArticleCategories)
                .ThenInclude(ac => ac.Category)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var isAuthor = article.AuthorId == currentUser.Id;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền chỉnh sửa bài viết của tác giả khác.", ErrorCodes.ArticleForbiddenEdit);
        }

        var oldSlug = article.Slug;

        // Nếu bài viết đang chờ tòa soạn phê duyệt (PendingCompanyReview) mà tác giả chỉnh sửa lại:
        // Tự động thu hồi về Bản nháp (Draft) để đảm bảo Ban biên tập không duyệt nhầm nội dung chưa kiểm chứng
        if (article.Status == ArticleStatus.PendingCompanyReview)
        {
            article.Status = ArticleStatus.Draft;
        }

        // 1. Cập nhật Tiêu đề & Tái tạo Slug nếu có thay đổi
        // Lưu ý SEO: Nếu bài viết đã xuất bản (Published), giữ nguyên Slug cũ để tránh làm chết liên kết/SEO backlinks
        if (!string.IsNullOrWhiteSpace(request.Title) && request.Title.Trim() != article.Title)
        {
            var newTitle = request.Title.Trim();
            article.Title = newTitle;

            if (article.Status != ArticleStatus.Published)
            {
                var baseSlug = SlugHelper.GenerateSlug(newTitle, 300);
                if (string.IsNullOrWhiteSpace(baseSlug))
                {
                    baseSlug = $"bai-viet-{article.Id:N}"[..20];
                }

                var uniqueSlug = await GenerateUniqueSlugAsync(article.Id, baseSlug, cancellationToken);
                article.Slug = uniqueSlug;
            }
        }

        // 2. Cập nhật Nội dung & Tính lại reading time nếu có thay đổi
        if (!string.IsNullOrWhiteSpace(request.ContentHtml))
        {
            var sanitizedHtml = htmlSanitizer.Sanitize(request.ContentHtml);
            if (string.IsNullOrWhiteSpace(sanitizedHtml))
            {
                throw new BadRequestException("Nội dung bài viết không hợp lệ sau khi làm sạch.", ErrorCodes.InvalidRequest);
            }

            article.ContentHtml = sanitizedHtml;
            article.ReadingTimeMinutes = ReadingTimeCalculator.Calculate(sanitizedHtml);
        }

        // 3. Cập nhật Ảnh bìa
        if (request.CoverImageId.HasValue && request.CoverImageId != article.CoverImageId)
        {
            var coverImage = await dbContext.MediaFiles
                .FirstOrDefaultAsync(m => m.Id == request.CoverImageId.Value, cancellationToken);

            if (coverImage is null)
            {
                throw NotFoundException.WithErrorCode("Ảnh bìa không tồn tại trong hệ thống.", ErrorCodes.MediaNotFound);
            }

            article.CoverImageId = request.CoverImageId;
            article.CoverImage = coverImage;
        }

        // 4. Cập nhật Nguồn tin
        if (request.SourceName != null)
        {
            article.SourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim();
        }

        if (request.SourceUrl != null)
        {
            article.SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim();
        }

        // 5. Cập nhật Danh mục
        var currentCategories = article.ArticleCategories.Select(ac => ac.Category!).Where(c => c != null).ToList();
        if (request.CategoryIds != null)
        {
            var distinctIds = request.CategoryIds.Distinct().ToList();
            var newCategories = await dbContext.Categories
                .Where(c => distinctIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            if (newCategories.Count != distinctIds.Count)
            {
                throw NotFoundException.WithErrorCode("Một hoặc nhiều chuyên mục đã chọn không tồn tại.", ErrorCodes.CategoryNotFound);
            }

            // Xóa quan hệ cũ
            dbContext.ArticleCategories.RemoveRange(article.ArticleCategories);

            // Gán quan hệ mới
            article.ArticleCategories.Clear();
            foreach (var category in newCategories)
            {
                article.ArticleCategories.Add(new ArticleCategory
                {
                    ArticleId = article.Id,
                    CategoryId = category.Id,
                    IsAiAssigned = false,
                    Category = category
                });
            }

            currentCategories = newCategories;
        }

        article.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await dbContext.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("Đường dẫn (slug) của bài viết đã tồn tại.", ErrorCodes.ArticleSlugConflict);
        }

        // Vô hiệu hóa Redis Cache
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(oldSlug), cancellationToken);
        if (oldSlug != article.Slug)
        {
            await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(article.Slug), cancellationToken);
        }
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleDetail(article.Id), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleRelated(article.Id), cancellationToken);

        // Nếu bài viết đã xuất bản, cập nhật lại vector embedding ngầm để tìm kiếm ngữ nghĩa luôn đồng bộ
        if (article.Status == ArticleStatus.Published)
        {
            try
            {
                backgroundJobService?.Enqueue<IAiBackgroundJobs>(job =>
                    job.GenerateArticleEmbeddingAsync(article.Id, CancellationToken.None));
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Lỗi khi đưa tác vụ cập nhật vector nhúng vào hàng đợi cho bài viết {ArticleId}", article.Id);
            }
        }

        return new ArticleDetailDto(
            article.Id,
            article.Title,
            article.Slug,
            article.ContentHtml,
            article.Status.ToString(),
            new AuthorSummaryDto(article.Author!.Id, article.Author.FullName, article.Author.AvatarUrl),
            article.CompanyId,
            article.SourceName,
            article.SourceUrl,
            article.CoverImage is null ? null : new CoverImageDto(
                article.CoverImage.Id,
                article.CoverImage.Url,
                article.CoverImage.Width,
                article.CoverImage.Height,
                article.CoverImage.Format),
            article.ReadingTimeMinutes,
            article.ViewCount,
            currentCategories.Select(c => new CategorySummaryDto(c.Id, c.Name, c.Slug)).ToList(),
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt);
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid articleId, string baseSlug, CancellationToken cancellationToken)
    {
        var slugPattern = baseSlug + "%";
        var existingSlugs = await dbContext.Articles
            .AsNoTracking()
            .Where(a => a.Id != articleId && (a.Slug == baseSlug || EF.Functions.Like(a.Slug, slugPattern)))
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
}
