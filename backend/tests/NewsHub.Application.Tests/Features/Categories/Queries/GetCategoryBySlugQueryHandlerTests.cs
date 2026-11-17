namespace NewsHub.Application.Tests.Features.Categories.Queries;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Queries.GetCategoryBySlug;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetCategoryBySlugQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly GetCategoryBySlugQueryValidator _validator = new();

    [Fact]
    public async Task Handle_WhenCategoryExists_ShouldReturnCategoryResponseDto()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categories = new List<Category>
        {
            new()
            {
                Id = categoryId,
                Name = "Công nghệ",
                Slug = "cong-nghe",
                Description = "Tin công nghệ và AI",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var mockDbSet = categories.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(mockDbSet.Object);

        var handler = new GetCategoryBySlugQueryHandler(_dbContextMock.Object);

        // Act
        var result = await handler.Handle(new GetCategoryBySlugQuery("cong-nghe"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(categoryId, result.Id);
        Assert.Equal("Công nghệ", result.Name);
        Assert.Equal("cong-nghe", result.Slug);
        Assert.Equal("Tin công nghệ và AI", result.Description);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var handler = new GetCategoryBySlugQueryHandler(_dbContextMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetCategoryBySlugQuery("non-existent-slug"), CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNotFound, exception.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_WithInvalidSlug_ShouldHaveValidationError(string? slug)
    {
        var query = new GetCategoryBySlugQuery(slug!);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Validator_WithValidSlug_ShouldNotHaveValidationError()
    {
        var query = new GetCategoryBySlugQuery("cong-nghe");
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
