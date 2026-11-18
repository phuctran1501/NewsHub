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
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveBookmarkCommand>
{
    public async Task Handle(RemoveBookmarkCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để bỏ lưu bài viết.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id!.Value;

        var bookmark = await dbContext.UserBookmarks
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.ArticleId == request.ArticleId, cancellationToken);

        if (bookmark is null)
        {
            // Idempotent: Nếu bookmark không tồn tại hoặc đã bị xóa trước đó, coi như thành công
            return;
        }

        dbContext.UserBookmarks.Remove(bookmark);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
