namespace NewsHub.Application.Tests.Features.Categories.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Commands.UpdateCategory;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly UpdateCategoryCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidNameUpdate_ShouldUpdateNameAndRegenerateSlugAndInvalidateCache()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Thời sự cũ",
            Slug = "thoi-su-cu",
            Description = "Mô tả cũ",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var categories = new List<Category> { category }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new UpdateCategoryCommand(categoryId, "Thời sự & Chính trị", "Mô tả mới");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Thời sự & Chính trị", result.Name);
        Assert.Equal("thoi-su-chinh-tri", result.Slug);
        Assert.Equal("Mô tả mới", result.Description);
        Assert.NotNull(category.UpdatedAt);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(CategoryCacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new UpdateCategoryCommand(Guid.NewGuid(), "Tên mới", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNewNameConflictsWithAnotherCategory_ShouldThrowConflictException()
    {
        // Arrange
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();

        var categories = new List<Category>
        {
            new() { Id = categoryId1, Name = "Thời sự", Slug = "thoi-su" },
            new() { Id = categoryId2, Name = "Kinh doanh", Slug = "kinh-doanh" }
        }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        // Cố gắng đổi category 1 thành tên của category 2
        var command = new UpdateCategoryCommand(categoryId1, "kinh doanh", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryConflict, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateConcurrencyExceptionOccurs_ShouldThrowNotFoundException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Cũ", Slug = "cu" };
        var categories = new List<Category> { category }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Concurrency conflict"));

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new UpdateCategoryCommand(categoryId, "Mới", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenDatabaseUniqueConstraintViolationOccurs_ShouldThrowConflictException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Cũ", Slug = "cu" };
        var categories = new List<Category> { category }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Duplicate", new Exception("23505: duplicate key")));

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new UpdateCategoryCommand(categoryId, "Mới", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryConflict, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUnrelatedDbUpdateExceptionOccurs_ShouldRethrowOriginalException()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Cũ", Slug = "cu" };
        var categories = new List<Category> { category }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Database error", new Exception("Disk full")));

        var handler = new UpdateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new UpdateCategoryCommand(categoryId, "Mới", null);

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void Validator_WhenNoFieldsProvided_ShouldHaveValidationError()
    {
        var command = new UpdateCategoryCommand(Guid.NewGuid(), null, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validator_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new UpdateCategoryCommand(Guid.Empty, "Tên mới", null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}
