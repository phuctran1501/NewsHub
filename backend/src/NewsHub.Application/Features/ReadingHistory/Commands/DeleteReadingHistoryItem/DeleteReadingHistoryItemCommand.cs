namespace NewsHub.Application.Features.ReadingHistory.Commands.DeleteReadingHistoryItem;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record DeleteReadingHistoryItemCommand(Guid HistoryId) : IRequest;

public sealed class DeleteReadingHistoryItemCommandValidator : AbstractValidator<DeleteReadingHistoryItemCommand>
{
    public DeleteReadingHistoryItemCommandValidator()
    {
        RuleFor(x => x.HistoryId)
            .NotEmpty().WithMessage("Định danh lịch sử đọc không được để trống.");
    }
}

public sealed class DeleteReadingHistoryItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<DeleteReadingHistoryItemCommand>
{
    public async Task Handle(DeleteReadingHistoryItemCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xóa lịch sử đọc.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        // Xóa trực tiếp qua ExecuteDeleteAsync để loại bỏ nguy cơ Concurrency Exception
        var deletedCount = await dbContext.ReadingHistories
            .Where(h => h.Id == request.HistoryId && h.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount == 0)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bản ghi lịch sử đọc yêu cầu.", ErrorCodes.ReadingHistoryNotFound);
        }
    }
}
