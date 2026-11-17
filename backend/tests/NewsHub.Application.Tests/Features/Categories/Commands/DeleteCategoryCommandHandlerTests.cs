namespace NewsHub.Application.Tests.Features.Categories.Commands;

using FluentValidation.TestHelper;
using MediatR;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Commands.DeleteCategory;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly DeleteCategoryCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WhenCategoryExists_ShouldRemoveFromDatabaseAndInvalidateCache()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Khoa học",
            Slug = "khoa-hoc"
        };

        var categories = new List<Category> { category }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new DeleteCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _dbContextMock.Verify(db => db.Categories.Remove(category), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(CategoryCacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var handler = new DeleteCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteCategoryCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNotFound, exception.ErrorCode);
    }

    [Fact]
    public void Validator_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new DeleteCategoryCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_WithValidId_ShouldNotHaveValidationError()
    {
        var command = new DeleteCategoryCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
