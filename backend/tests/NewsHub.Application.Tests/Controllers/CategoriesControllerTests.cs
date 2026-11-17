namespace NewsHub.Application.Tests.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewsHub.API.Controllers;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Categories.Commands.CreateCategory;
using NewsHub.Application.Features.Categories.Commands.DeleteCategory;
using NewsHub.Application.Features.Categories.Commands.UpdateCategory;
using NewsHub.Application.Features.Categories.DTOs;
using NewsHub.Application.Features.Categories.Queries.GetCategories;
using NewsHub.Application.Features.Categories.Queries.GetCategoryBySlug;
using Xunit;

public class CategoriesControllerTests
{
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly Mock<ICorrelationIdProvider> _correlationIdProviderMock = new();
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _correlationIdProviderMock.Setup(p => p.GetCorrelationId()).Returns("corr-cat-123");
        _controller = new CategoriesController(_mediatorMock.Object, _correlationIdProviderMock.Object);
    }

    [Fact]
    public async Task GetCategories_ShouldSendQueryAndReturnOk()
    {
        // Arrange
        var dtoList = new List<CategoryResponseDto>
        {
            new(Guid.NewGuid(), "Thời sự", "thoi-su", "Mô tả", 5, DateTimeOffset.UtcNow)
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtoList);

        // Act
        var result = await _controller.GetCategories(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<IReadOnlyList<CategoryResponseDto>>>(okResult.Value);
        Assert.Single(envelope.Data);
        Assert.Equal("corr-cat-123", envelope.CorrelationId);
    }

    [Fact]
    public async Task GetCategoryBySlug_ShouldSendQueryAndReturnOk()
    {
        // Arrange
        var dto = new CategoryResponseDto(Guid.NewGuid(), "Công nghệ", "cong-nghe", "Mô tả", 10, DateTimeOffset.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetCategoryBySlugQuery>(q => q.Slug == "cong-nghe"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetCategoryBySlug("cong-nghe", CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<CategoryResponseDto>>(okResult.Value);
        Assert.Equal("Công nghệ", envelope.Data.Name);
        Assert.Equal("corr-cat-123", envelope.CorrelationId);
    }

    [Fact]
    public async Task CreateCategory_ShouldSendCommandAndReturnCreatedWithLocation()
    {
        // Arrange
        var command = new CreateCategoryCommand("Kinh doanh", "Tin tài chính");
        var dto = new CategoryResponseDto(Guid.NewGuid(), "Kinh doanh", "kinh-doanh", "Tin tài chính", 0, DateTimeOffset.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.CreateCategory(command, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/v1/categories/kinh-doanh", createdResult.Location);
        var envelope = Assert.IsType<ApiResponse<CategoryResponseDto>>(createdResult.Value);
        Assert.Equal("Kinh doanh", envelope.Data.Name);
    }

    [Fact]
    public async Task UpdateCategory_ShouldSendCommandAndReturnOk()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Kinh tế", "Mô tả mới");
        var dto = new CategoryResponseDto(categoryId, "Kinh tế", "kinh-te", "Mô tả mới", 0, DateTimeOffset.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.Is<UpdateCategoryCommand>(c => c.Id == categoryId && c.Name == "Kinh tế"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.UpdateCategory(categoryId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<CategoryResponseDto>>(okResult.Value);
        Assert.Equal("Kinh tế", envelope.Data.Name);
    }

    [Fact]
    public async Task DeleteCategory_ShouldSendCommandAndReturnOk()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteCategoryCommand>(c => c.Id == categoryId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _controller.DeleteCategory(categoryId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var correlationIdProp = okResult.Value.GetType().GetProperty("CorrelationId");
        Assert.NotNull(correlationIdProp);
        var correlationId = correlationIdProp.GetValue(okResult.Value) as string;
        Assert.Equal("corr-cat-123", correlationId);
    }
}
