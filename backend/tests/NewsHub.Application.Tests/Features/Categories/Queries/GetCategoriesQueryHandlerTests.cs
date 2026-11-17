namespace NewsHub.Application.Tests.Features.Categories.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Application.Features.Categories.DTOs;
using NewsHub.Application.Features.Categories.Queries.GetCategories;
using NewsHub.Domain.Entities;
using Xunit;

public class GetCategoriesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldQueryDatabaseAndPopulateCache()
    {
        // Arrange
        var categories = new List<Category>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Công nghệ",
                Slug = "cong-nghe",
                Description = "Tin công nghệ",
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Thời sự",
                Slug = "thoi-su",
                Description = "Tin thời sự",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var mockDbSet = categories.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(mockDbSet.Object);

        // Giả lập GetOrCreateAsync thực thi delegate factory khi cache miss
        _cacheServiceMock
            .Setup(c => c.GetOrCreateAsync(
                CategoryCacheKeys.AllCategories,
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<CategoryResponseDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<CategoryResponseDto>>>, TimeSpan?, CancellationToken>(
                (_, factory, _, ct) => factory(ct));

        var handler = new GetCategoriesQueryHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Công nghệ", result[0].Name);
        Assert.Equal("Thời sự", result[1].Name);
        _cacheServiceMock.Verify(c => c.GetOrCreateAsync(
            CategoryCacheKeys.AllCategories,
            It.IsAny<Func<CancellationToken, Task<IReadOnlyList<CategoryResponseDto>>>>(),
            It.Is<TimeSpan?>(t => t == TimeSpan.FromHours(24)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDataDirectly()
    {
        // Arrange
        var cachedCategories = new List<CategoryResponseDto>
        {
            new(Guid.NewGuid(), "Thể thao", "the-thao", "Tin thể thao", 10, DateTimeOffset.UtcNow)
        };

        _cacheServiceMock
            .Setup(c => c.GetOrCreateAsync(
                CategoryCacheKeys.AllCategories,
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<CategoryResponseDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedCategories);

        var handler = new GetCategoriesQueryHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Thể thao", result[0].Name);
        // DbContext không bị gọi tới khi cache hit
        _dbContextMock.Verify(db => db.Categories, Times.Never);
    }
}
