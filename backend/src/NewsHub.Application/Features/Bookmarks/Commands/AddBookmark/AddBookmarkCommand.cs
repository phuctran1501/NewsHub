namespace NewsHub.Application.Features.Bookmarks.Commands.AddBookmark;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record AddBookmarkCommand(Guid ArticleId) : IRequest;

public sealed class AddBookmarkCommandValidator : AbstractValidator<AddBookmarkCommand>
{
    public AddBookmarkCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class AddBookmarkCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<AddBookmarkCommand>
{
    public async Task Handle(AddBookmarkCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để lưu bài viết.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        // 1. Kiểm tra bài viết tồn tại và đang ở trạng thái Published
        var article = await dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        if (article.Status != ArticleStatus.Published)
        {
            throw new BadRequestException("Chỉ được lưu các bài viết đã xuất bản.", ErrorCodes.ArticleNotPublished);
        }

        // 2. Kiểm tra Idempotent: nếu đã lưu rồi thì kết thúc thành công 200 OK
        var existingBookmark = await dbContext.UserBookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(ub => ub.UserId == userId && ub.ArticleId == request.ArticleId, cancellationToken);

        if (existingBookmark is not null)
        {
            return;
        }

        // 3. Tạo mới bản ghi bookmark
        var bookmark = new UserBookmark
        {
            UserId = userId,
            ArticleId = request.ArticleId,
            SavedAt = DateTimeOffset.UtcNow
        };

        try
        {
            dbContext.UserBookmarks.Add(bookmark);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Bắt race condition nếu hai request đồng thời cùng lưu bài viết
            // Idempotent: không coi là lỗi
        }
        catch (DbUpdateException)
        {
            // Nếu bài viết bị xóa giữa lúc check và insert -> vi phạm Foreign Key
            var stillExists = await dbContext.Articles
                .AsNoTracking()
                .AnyAsync(a => a.Id == request.ArticleId, cancellationToken);

            if (!stillExists)
            {
                throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
            }

            throw;
        }
    }
}
