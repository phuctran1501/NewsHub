using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    public record SampleCommand(string Message) : IRequest<string>;

    private readonly Mock<ILogger<LoggingBehavior<SampleCommand, string>>> _mockLogger;
    private readonly Mock<ICorrelationIdProvider> _mockCorrelationIdProvider;
    private readonly LoggingBehavior<SampleCommand, string> _behavior;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<SampleCommand, string>>>();
        _mockCorrelationIdProvider = new Mock<ICorrelationIdProvider>();
        _mockCorrelationIdProvider.Setup(p => p.GetCorrelationId()).Returns("test-correlation-id-123");

        _behavior = new LoggingBehavior<SampleCommand, string>(
            _mockLogger.Object,
            _mockCorrelationIdProvider.Object);
    }

    [Fact]
    public async Task Handle_WhenRequestSucceeds_ShouldLogInformationAndReturnResult()
    {
        // Arrange
        var command = new SampleCommand("Hello");
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Response");

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.Equal("Response", result);

        // Verify start log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Handling SampleCommand") && v.ToString()!.Contains("test-correlation-id-123")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify end log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Handled SampleCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExecutionExceedsThreshold_ShouldLogWarning()
    {
        // Arrange
        var command = new SampleCommand("Slow command");
        RequestHandlerDelegate<string> next = async (ct) =>
        {
            await Task.Delay(550); // Vượt ngưỡng 500ms
            return "Slow Response";
        };

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.Equal("Slow Response", result);

        // Verify warning log for long running request
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Long Running Request: SampleCommand")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var command = new SampleCommand("Error command");
        var expectedException = new InvalidOperationException("Something went wrong");
        RequestHandlerDelegate<string> next = (ct) => throw expectedException;

        // Act & Assert
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.Handle(command, next, CancellationToken.None));

        Assert.Same(expectedException, thrown);

        // Verify error log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Request failure SampleCommand")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
