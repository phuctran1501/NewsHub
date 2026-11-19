namespace NewsHub.Application.Features.Ai.Commands.RegenerateArticleAiSummary;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record RegenerateArticleAiSummaryCommand(Guid ArticleId) : IRequest;

public sealed class RegenerateArticleAiSummaryCommandValidator : AbstractValidator<RegenerateArticleAiSummaryCommand>
{
    public RegenerateArticleAiSummaryCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class RegenerateArticleAiSummaryCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IBackgroundJobService backgroundJobService) : IRequestHandler<RegenerateArticleAiSummaryCommand>
{
    public async Task Handle(
        RegenerateArticleAiSummaryCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để yêu cầu tạo lại bản tóm tắt AI.", ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (article == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var isAuthor = article.AuthorId == currentUser.Id;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("Chỉ tác giả bài viết hoặc quản trị viên mới có quyền tạo lại bản tóm tắt AI.", ErrorCodes.PermissionDenied);
        }

        // Đẩy tác vụ vào hàng đợi Hangfire
        backgroundJobService.Enqueue<IAiBackgroundJobs>(job =>
            job.GenerateArticleSummaryAsync(article.Id, CancellationToken.None));
    }
}
