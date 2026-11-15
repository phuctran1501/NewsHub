using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

namespace NewsHub.Application.Tests.Common;

public class ApiResponseFactoryTests
{
    private const string TestCorrelationId = "test-corr-id-123";

    [Fact]
    public void Success_ShouldCreateValidApiResponse_WithDefaultMeta()
    {
        // Arrange
        var testData = new { Name = "NewsHub", Version = "2.0" };

        // Act
        var response = ApiResponseFactory.Success(testData, TestCorrelationId);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(testData, response.Data);
        Assert.Equal(TestCorrelationId, response.CorrelationId);
        Assert.NotNull(response.Meta);
    }

    [Fact]
    public void Success_ShouldPreserveCustomMeta_WhenProvided()
    {
        // Arrange
        var testData = "test string";
        var customMeta = new { Server = "Staging-1" };

        // Act
        var response = ApiResponseFactory.Success(testData, TestCorrelationId, customMeta);

        // Assert
        Assert.Equal(customMeta, response.Meta);
    }

    [Fact]
    public void Paged_WithValidParameters_ShouldCalculatePagingMetadataCorrectly()
    {
        // Arrange: Trang 2 trong tổng số 5 trang
        var items = new List<string> { "Item 1", "Item 2" };
        var pageNumber = 2;
        var pageSize = 2;
        var totalCount = 9; // 9 / 2 = 4.5 -> totalPages = 5

        // Act
        var response = ApiResponseFactory.Paged(items, pageNumber, pageSize, totalCount, TestCorrelationId);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(items, response.Data);
        Assert.Equal(TestCorrelationId, response.CorrelationId);
        Assert.Equal(2, response.Meta.PageNumber);
        Assert.Equal(2, response.Meta.PageSize);
        Assert.Equal(9, response.Meta.TotalCount);
        Assert.Equal(5, response.Meta.TotalPages);
        Assert.True(response.Meta.HasPreviousPage);
        Assert.True(response.Meta.HasNextPage);
    }

    [Fact]
    public void Paged_FirstPage_ShouldHaveNoPreviousPage()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };

        // Act
        var response = ApiResponseFactory.Paged(items, pageNumber: 1, pageSize: 3, totalCount: 10, TestCorrelationId);

        // Assert
        Assert.False(response.Meta.HasPreviousPage);
        Assert.True(response.Meta.HasNextPage);
    }

    [Fact]
    public void Paged_LastPage_ShouldHaveNoNextPage()
    {
        // Arrange
        var items = new List<int> { 7, 8 };

        // Act
        var response = ApiResponseFactory.Paged(items, pageNumber: 4, pageSize: 2, totalCount: 8, TestCorrelationId);

        // Assert
        Assert.Equal(4, response.Meta.TotalPages);
        Assert.True(response.Meta.HasPreviousPage);
        Assert.False(response.Meta.HasNextPage);
    }

    [Fact]
    public void Paged_EmptyData_ShouldHaveZeroTotalPagesAndNoNavigation()
    {
        // Arrange
        var items = Enumerable.Empty<string>();

        // Act
        var response = ApiResponseFactory.Paged(items, pageNumber: 1, pageSize: 10, totalCount: 0, TestCorrelationId);

        // Assert
        Assert.Equal(0, response.Meta.TotalPages);
        Assert.False(response.Meta.HasPreviousPage);
        Assert.False(response.Meta.HasNextPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Paged_WhenPageNumberIsZeroOrNegative_ShouldThrowBadRequestException(int invalidPageNumber)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<BadRequestException>(() =>
            ApiResponseFactory.Paged(new[] { 1 }, invalidPageNumber, pageSize: 10, totalCount: 10, TestCorrelationId));

        Assert.Equal(AppErrorKind.BadRequest, ex.ErrorKind);
        Assert.Equal(ErrorCodes.InvalidRequest, ex.ErrorCode);
        Assert.Contains("pageNumber", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Paged_WhenPageSizeIsZeroOrNegative_ShouldThrowBadRequestException(int invalidPageSize)
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<BadRequestException>(() =>
            ApiResponseFactory.Paged(new[] { 1 }, pageNumber: 1, invalidPageSize, totalCount: 10, TestCorrelationId));

        Assert.Equal(AppErrorKind.BadRequest, ex.ErrorKind);
        Assert.Equal(ErrorCodes.InvalidRequest, ex.ErrorCode);
        Assert.Contains("pageSize", ex.Message);
    }
}
