namespace NewsHub.Application.Tests.Features.Ai.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Application.Features.Ai.Queries.SuggestCategories;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class SuggestCategoriesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();

    private SuggestCategoriesQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _aiServiceMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        var handler = CreateHandler();
        var query = new SuggestCategoriesQuery("Tiêu đề", "Nội dung trích đoạn");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsReader_ShouldThrowForbiddenException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Journalist)).Returns(false);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);
        var handler = CreateHandler();
        var query = new SuggestCategoriesQuery("Tiêu đề", "Nội dung trích đoạn");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNoCategoriesInSystem_ShouldReturnEmptyListWithoutCallingAi()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Journalist)).Returns(true);

        var emptyCategories = new List<Category>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(emptyCategories.Object);

        var handler = CreateHandler();
        var query = new SuggestCategoriesQuery("Tiêu đề", "Nội dung trích đoạn");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Suggestions);
        _aiServiceMock.Verify(a => a.SuggestCategoriesAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<IReadOnlyList<CategoryCandidateDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenJournalistAndCategoriesExist_ShouldCallAiServiceAndReturnSuggestions()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Journalist)).Returns(true);

        var catId = Guid.NewGuid();
        var categories = new List<Category>
        {
            new() { Id = catId, Name = "Công nghệ", Slug = "cong-nghe" }
        }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);

        var expectedSuggestions = new List<CategorySuggestionDto>
        {
            new(catId, "Công nghệ", "cong-nghe", 0.95f, "Phù hợp với nội dung AI")
        };

        _aiServiceMock.Setup(a => a.SuggestCategoriesAsync(
            "Tiêu đề AI",
            "Nội dung AI",
            It.IsAny<IReadOnlyList<CategoryCandidateDto>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSuggestions);

        var handler = CreateHandler();
        var query = new SuggestCategoriesQuery("Tiêu đề AI", "Nội dung AI");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Suggestions);
        Assert.Equal("Công nghệ", result.Suggestions[0].CategoryName);
        Assert.Equal(0.95f, result.Suggestions[0].Confidence);
    }

    [Theory]
    [InlineData("", "Nội dung trích đoạn", false)]
    [InlineData("Tiêu đề", "", false)]
    [InlineData("Tiêu đề hợp lệ", "Nội dung hợp lệ", true)]
    public void Validator_ValidationRules_ShouldValidateCorrectly(string title, string snippet, bool expectedValid)
    {
        var validator = new SuggestCategoriesQueryValidator();
        var query = new SuggestCategoriesQuery(title, snippet);
        var result = validator.Validate(query);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
