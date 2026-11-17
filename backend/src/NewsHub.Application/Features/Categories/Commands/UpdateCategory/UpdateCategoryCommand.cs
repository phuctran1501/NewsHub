namespace NewsHub.Application.Features.Categories.Commands.UpdateCategory;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Application.Features.Categories.DTOs;
using NewsHub.Domain.Common;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string? Name,
    string? Description) : IRequest<CategoryResponseDto>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Định danh chuyên mục không được để trống.");

        RuleFor(x => x)
            .Must(x => x.Name != null || x.Description != null)
            .WithMessage("Phải cung cấp ít nhất một trường thông tin để cập nhật.");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty()
                .WithMessage("Tên chuyên mục không được để trống.")
                .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2 && name.Trim().Length <= 100)
                .WithMessage("Tên chuyên mục phải từ 2 đến 100 ký tự.");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(500)
                .WithMessage("Mô tả chuyên mục không được vượt quá 500 ký tự.");
        });
    }
}

public sealed class UpdateCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<UpdateCategoryCommand, CategoryResponseDto>
{
    public async Task<CategoryResponseDto> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy chuyên mục tin tức.", ErrorCodes.CategoryNotFound);
        }

        if (request.Name != null)
        {
            var normalizedName = request.Name.Trim();
            var newSlug = SlugHelper.GenerateSlug(normalizedName);

            if (string.IsNullOrWhiteSpace(newSlug))
            {
                throw new ConflictException("Không thể tạo đường dẫn (slug) hợp lệ từ tên chuyên mục đã nhập.", ErrorCodes.InvalidRequest);
            }

            var conflict = await dbContext.Categories
                .AnyAsync(c => c.Id != request.Id && (c.Name.ToLower() == normalizedName.ToLower() || c.Slug == newSlug), cancellationToken);

            if (conflict)
            {
                throw new ConflictException("Tên hoặc đường dẫn (slug) của chuyên mục đã tồn tại trong hệ thống.", ErrorCodes.CategoryConflict);
            }

            category.Name = normalizedName;
            category.Slug = newSlug;
        }

        if (request.Description != null)
        {
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        category.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy chuyên mục tin tức.", ErrorCodes.CategoryNotFound);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("Tên hoặc đường dẫn (slug) của chuyên mục đã tồn tại trong hệ thống.", ErrorCodes.CategoryConflict);
        }

        // Xóa cache danh mục để đảm bảo tính nhất quán tức thì
        await cacheService.RemoveAsync(CategoryCacheKeys.AllCategories, cancellationToken);

        return new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            0,
            category.CreatedAt);
    }
}
