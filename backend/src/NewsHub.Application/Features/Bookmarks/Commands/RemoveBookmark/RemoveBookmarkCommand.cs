namespace NewsHub.Application.Features.Bookmarks.Commands.RemoveBookmark;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record RemoveBookmarkCommand(Guid ArticleId) : IRequest;

public sealed class RemoveBookmarkCommandValidator : AbstractValidator<RemoveBookmarkCommand>
{
    public RemoveBookmarkCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class RemoveBookmarkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<RemoveBookmarkCommand>
{
    public async Task Handle(RemoveBookmarkCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để bỏ lưu bài viết.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        // Xóa trực tiếp qua ExecuteDeleteAsync để bảo đảm an toàn trước Concurrency và Idempotent
        await dbContext.UserBookmarks
            .Where(ub => ub.UserId == userId && ub.ArticleId == request.ArticleId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
