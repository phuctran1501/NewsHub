namespace NewsHub.Application.Features.Categories.Commands.DeleteCategory;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Định danh chuyên mục không được để trống.");
    }
}

public sealed class DeleteCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy chuyên mục tin tức.", ErrorCodes.CategoryNotFound);
        }

        // Ràng buộc toàn vẹn: Khi bảng article_categories được thêm ở NH-FEAT-04,
        // kiểm tra nếu có bài viết thuộc chuyên mục này thì chặn xóa và ném ConflictException(ErrorCodes.CategoryInUse).

        dbContext.Categories.Remove(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Không thể xóa chuyên mục đang chứa bài viết hoặc đang được sử dụng trong hệ thống.", ErrorCodes.CategoryInUse);
        }

        // Xóa cache danh mục
        await cacheService.RemoveAsync(CategoryCacheKeys.AllCategories, cancellationToken);

        return Unit.Value;
    }
}
