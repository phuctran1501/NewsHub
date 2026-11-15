using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NewsHub.API.Middleware;
using NewsHub.Application.Common.Exceptions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;
using Xunit;

namespace NewsHub.Application.Tests.Middleware;

public class ApiExceptionHandlerTests
{
    private readonly Mock<ILogger<ApiExceptionHandler>> _mockLogger;
    private readonly Mock<ICorrelationIdProvider> _mockCorrelationIdProvider;
    private readonly ApiExceptionHandler _handler;
    private const string ExpectedCorrelationId = "corr-test-guid-456";

    public ApiExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<ApiExceptionHandler>>();
        _mockCorrelationIdProvider = new Mock<ICorrelationIdProvider>();
        _mockCorrelationIdProvider.Setup(x => x.GetCorrelationId()).Returns(ExpectedCorrelationId);

        _handler = new ApiExceptionHandler(_mockLogger.Object, _mockCorrelationIdProvider.Object);
    }

    private async Task<(int StatusCode, ApiErrorResponse? ResponseBody)> ExecuteHandlerAsync(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var memoryStream = new MemoryStream();
        httpContext.Response.Body = memoryStream;

        var handled = await _handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        Assert.True(handled);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(memoryStream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (httpContext.Response.StatusCode, responseBody);
    }

    [Fact]
    public async Task TryHandleAsync_BadRequestException_ShouldReturn400WithCorrectEnvelope()
    {
        // Arrange
        var ex = new BadRequestException("Invalid parameter value provided.", ErrorCodes.InvalidRequest);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.InvalidRequest, body.Error.Code);
        Assert.Equal("Invalid parameter value provided.", body.Error.Message);
        Assert.Empty(body.Error.Details);
    }

    [Fact]
    public async Task TryHandleAsync_NotFoundException_ShouldReturn404WithCorrectEnvelope()
    {
        // Arrange
        var ex = new NotFoundException("Article", "slug-khong-ton-tai");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.ResourceNotFound, body.Error.Code);
        Assert.Contains("slug-khong-ton-tai", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_ConflictException_ShouldReturn409WithCorrectEnvelope()
    {
        // Arrange
        var ex = new ConflictException("User", "Email", "test@newshub.vn");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.Conflict, body.Error.Code);
        Assert.Contains("test@newshub.vn", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_ForbiddenException_ShouldReturn403WithCorrectEnvelope()
    {
        // Arrange
        var ex = new ForbiddenException("Cannot edit article of another author.", ErrorCodes.PermissionDenied);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.PermissionDenied, body.Error.Code);
        Assert.Equal("Cannot edit article of another author.", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_UnauthorizedException_ShouldReturn401WithCorrectEnvelope()
    {
        // Arrange
        var ex = new UnauthorizedException("Session has expired.", ErrorCodes.TokenExpired);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.TokenExpired, body.Error.Code);
        Assert.Equal("Session has expired.", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_GoneException_ShouldReturn410WithCorrectEnvelope()
    {
        // Arrange
        var ex = new GoneException("Refresh token was revoked.", ErrorCodes.TokenRevoked);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status410Gone, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.TokenRevoked, body.Error.Code);
        Assert.Equal("Refresh token was revoked.", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_AppValidationException_ShouldReturn422WithFieldErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required.", "Email format is invalid." },
            ["Password"] = new[] { "Password must be at least 8 characters." }
        };
        var ex = new AppValidationException(errors);

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.ValidationFailed, body.Error.Code);
        Assert.Equal(3, body.Error.Details.Count);

        var emailErrors = body.Error.Details.Where(d => d.Field == "Email").ToList();
        Assert.Equal(2, emailErrors.Count);
        Assert.Contains(emailErrors, e => e.Message == "Email is required.");
        Assert.Contains(emailErrors, e => e.Message == "Email format is invalid.");

        var passErrors = body.Error.Details.Where(d => d.Field == "Password").ToList();
        Assert.Single(passErrors);
        Assert.Equal("Password must be at least 8 characters.", passErrors[0].Message);
    }

    [Fact]
    public async Task TryHandleAsync_UnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        var ex = new UnauthorizedAccessException("Custom access denied.");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert: Ẩn thông điệp nội bộ (file path, system details) khỏi UnauthorizedAccessException
        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.Unauthorized, body.Error.Code);
        Assert.Equal("Authentication is required to access this resource.", body.Error.Message);
    }

    [Fact]
    public async Task TryHandleAsync_UnhandledException_ShouldReturn500AndSanitizeMessage()
    {
        // Arrange: Giả lập lỗi runtime nguy hiểm chứa connection string hoặc query nhạy cảm
        var ex = new InvalidOperationException("Fatal database crash: Host=10.0.0.1;Password=secret;");

        // Act
        var (statusCode, body) = await ExecuteHandlerAsync(ex);

        // Assert: Ẩn hoàn toàn chi tiết kỹ thuật khỏi client
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.NotNull(body);
        Assert.Equal(ExpectedCorrelationId, body.CorrelationId);
        Assert.Equal(ErrorCodes.InternalServerError, body.Error.Code);
        Assert.Equal("An unexpected error occurred. Please try again later.", body.Error.Message);
        Assert.Empty(body.Error.Details);
        Assert.DoesNotContain("Password", body.Error.Message);
    }
}
