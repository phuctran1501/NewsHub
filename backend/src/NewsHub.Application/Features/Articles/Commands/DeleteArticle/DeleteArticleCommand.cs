namespace NewsHub.Application.Features.Articles.Commands.DeleteArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record DeleteArticleCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteArticleCommandValidator : AbstractValidator<DeleteArticleCommand>
{
    public DeleteArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class DeleteArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICacheService cacheService) : IRequestHandler<DeleteArticleCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xóa bài viết.", ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var isAuthor = article.AuthorId == currentUser.Id;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền xóa bài viết của tác giả khác.", ErrorCodes.ArticleForbiddenEdit);
        }

        // Quy tắc nghiệp vụ tòa soạn: Bài viết đã xuất bản thuộc sở hữu của tòa soạn, phóng viên không được phép tự ý xóa
        if (article.CompanyId.HasValue && article.Status == ArticleStatus.Published && !isAdmin)
        {
            throw new ForbiddenException(
                "Bài viết đã xuất bản thuộc sở hữu của tòa soạn. Phóng viên không được phép xóa.",
                ErrorCodes.CannotDeletePublishedCompanyArticle);
        }

        var slug = article.Slug;
        var articleId = article.Id;

        dbContext.Articles.Remove(article);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        // Xóa cache Redis
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(slug), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleDetail(articleId), cancellationToken);
        await cacheService.RemoveAsync(ArticleCacheKeys.ArticleRelated(articleId), cancellationToken);

        return Unit.Value;
    }
}
