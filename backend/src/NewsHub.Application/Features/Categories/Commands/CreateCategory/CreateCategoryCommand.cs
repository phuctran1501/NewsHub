namespace NewsHub.Application.Features.Categories.Commands.CreateCategory;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Application.Features.Categories.DTOs;
using NewsHub.Domain.Common;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description) : IRequest<CategoryResponseDto>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tên chuyên mục không được để trống.")
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2 && name.Trim().Length <= 100)
            .WithMessage("Tên chuyên mục phải từ 2 đến 100 ký tự.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description!)
                .MaximumLength(500)
                .WithMessage("Mô tả chuyên mục không được vượt quá 500 ký tự.");
        });
    }
}

public sealed class CreateCategoryCommandHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<CreateCategoryCommand, CategoryResponseDto>
{
    public async Task<CategoryResponseDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var slug = SlugHelper.GenerateSlug(normalizedName);

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ConflictException("Không thể tạo đường dẫn (slug) hợp lệ từ tên chuyên mục đã nhập.", ErrorCodes.InvalidRequest);
        }

        var conflict = await dbContext.Categories
            .AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower() || c.Slug == slug, cancellationToken);

        if (conflict)
        {
            throw new ConflictException("Tên hoặc đường dẫn (slug) của chuyên mục đã tồn tại trong hệ thống.", ErrorCodes.CategoryConflict);
        }

        var category = new Category
        {
            Name = normalizedName,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Categories.Add(category);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("Tên hoặc đường dẫn (slug) của chuyên mục đã tồn tại trong hệ thống.", ErrorCodes.CategoryConflict);
        }

        // Vô hiệu hóa cache danh mục để độc giả nhận dữ liệu mới nhất
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
