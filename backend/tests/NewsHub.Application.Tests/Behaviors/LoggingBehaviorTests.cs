using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Interfaces;
using NSubstitute;
using Xunit;

namespace NewsHub.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestQuery, string>> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public LoggingBehaviorTests()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<TestQuery, string>>>();
        _correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
        _correlationIdProvider.GetCorrelationId().Returns("test-correlation-id");
    }

    public record TestQuery : IRequest<string>;

    [Fact]
    public async Task Handle_SuccessfulRequest_ShouldExecuteAndReturnResponse()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestQuery, string>(_logger, _correlationIdProvider);
        var query = new TestQuery();
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("QueryResult");

        // Act
        var result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.Should().Be("QueryResult");
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldRethrowException()
    {
        // Arrange
        var behavior = new LoggingBehavior<TestQuery, string>(_logger, _correlationIdProvider);
        var query = new TestQuery();
        var expectedException = new InvalidOperationException("Something bad happened");
        RequestHandlerDelegate<string> next = (ct) => throw expectedException;

        // Act
        var act = () => behavior.Handle(query, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Something bad happened");
    }
}
