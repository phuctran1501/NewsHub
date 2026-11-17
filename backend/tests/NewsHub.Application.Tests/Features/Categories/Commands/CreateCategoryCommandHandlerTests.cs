namespace NewsHub.Application.Tests.Features.Categories.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Categories.Commands.CreateCategory;
using NewsHub.Application.Features.Categories.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using Xunit;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateCategoryGenerateSlugAndInvalidateCache()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new CreateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new CreateCategoryCommand("Khoa học & Đời sống", "Chuyên mục tin tức khoa học");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Khoa học & Đời sống", result.Name);
        Assert.Equal("khoa-hoc-doi-song", result.Slug);
        Assert.Equal("Chuyên mục tin tức khoa học", result.Description);

        _dbContextMock.Verify(db => db.Categories.Add(It.Is<Category>(c =>
            c.Name == "Khoa học & Đời sống" &&
            c.Slug == "khoa-hoc-doi-song")), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(CategoryCacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameOrSlugAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var existingCategory = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Công nghệ",
            Slug = "cong-nghe"
        };

        var categories = new List<Category> { existingCategory }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var handler = new CreateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new CreateCategoryCommand("công nghệ", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryConflict, exception.ErrorCode);
        _dbContextMock.Verify(db => db.Categories.Add(It.IsAny<Category>()), Times.Never);
        _cacheServiceMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDatabaseUniqueConstraintViolationOccurs_ShouldThrowConflictException()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Duplicate", new Exception("23505: duplicate key")));

        var handler = new CreateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new CreateCategoryCommand("Kinh doanh", null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.CategoryConflict, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUnrelatedDbUpdateExceptionOccurs_ShouldRethrowOriginalException()
    {
        // Arrange
        var categories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Database error", new Exception("Connection lost")));

        var handler = new CreateCategoryCommandHandler(_dbContextMock.Object, _cacheServiceMock.Object);
        var command = new CreateCategoryCommand("Kinh doanh", null);

        // Act & Assert
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")] // Quá ngắn (< 2 ký tự)
    public void Validator_WithInvalidName_ShouldHaveValidationError(string name)
    {
        var command = new CreateCategoryCommand(name, null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_WithNameExceeding100Chars_ShouldHaveValidationError()
    {
        var command = new CreateCategoryCommand(new string('a', 101), null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_WithDescriptionExceeding500Chars_ShouldHaveValidationError()
    {
        var command = new CreateCategoryCommand("Công nghệ", new string('a', 501));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validator_WithValidData_ShouldNotHaveValidationError()
    {
        var command = new CreateCategoryCommand("Kinh doanh", "Tin tài chính, chứng khoán");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
