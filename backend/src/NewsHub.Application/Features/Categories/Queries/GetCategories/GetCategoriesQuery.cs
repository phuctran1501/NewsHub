namespace NewsHub.Application.Features.Categories.Queries.GetCategories;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Application.Features.Categories.DTOs;

public sealed record GetCategoriesQuery : IRequest<IReadOnlyList<CategoryResponseDto>>;

public sealed class GetCategoriesQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService) : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryResponseDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<CategoryResponseDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await cacheService.GetOrCreateAsync<IReadOnlyList<CategoryResponseDto>>(
            CategoryCacheKeys.AllCategories,
            async ct =>
            {
                var categories = await dbContext.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Select(c => new CategoryResponseDto(
                        c.Id,
                        c.Name,
                        c.Slug,
                        c.Description,
                        0, // articleCount sẽ được liên kết với ArticleCategories trong NH-FEAT-04
                        c.CreatedAt))
                    .ToListAsync(ct);

                return categories;
            },
            CacheTtl,
            cancellationToken);
    }
}
