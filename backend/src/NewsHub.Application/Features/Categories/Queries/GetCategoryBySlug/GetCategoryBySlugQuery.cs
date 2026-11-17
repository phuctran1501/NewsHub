namespace NewsHub.Application.Features.Categories.Queries.GetCategoryBySlug;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetCategoryBySlugQuery(string Slug) : IRequest<CategoryResponseDto>;

public sealed class GetCategoryBySlugQueryValidator : AbstractValidator<GetCategoryBySlugQuery>
{
    public GetCategoryBySlugQueryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .WithMessage("Đường dẫn (slug) của chuyên mục không được để trống.")
            .MaximumLength(120)
            .WithMessage("Đường dẫn (slug) không được vượt quá 120 ký tự.");
    }
}

public sealed class GetCategoryBySlugQueryHandler(
    IApplicationDbContext dbContext) : IRequestHandler<GetCategoryBySlugQuery, CategoryResponseDto>
{
    public async Task<CategoryResponseDto> Handle(
        GetCategoryBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == request.Slug.Trim().ToLowerInvariant(), cancellationToken);

        if (category is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy chuyên mục tin tức.", ErrorCodes.CategoryNotFound);
        }

        return new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            0, // articleCount sẽ được liên kết với ArticleCategories trong NH-FEAT-04
            category.CreatedAt);
    }
}
