using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Interfaces;
using Xunit;

namespace NewsHub.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<TransactionBehavior<NonTransactionalCommand, string>>> _mockNonTransactionalLogger;
    private readonly Mock<ILogger<TransactionBehavior<TransactionalCommand, string>>> _mockTransactionalLogger;

    public TransactionBehaviorTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockNonTransactionalLogger = new Mock<ILogger<TransactionBehavior<NonTransactionalCommand, string>>>();
        _mockTransactionalLogger = new Mock<ILogger<TransactionBehavior<TransactionalCommand, string>>>();
    }

    public record NonTransactionalCommand : IRequest<string>;
    public record TransactionalCommand : IRequest<string>, ITransactionalRequest;

    [Fact]
    public async Task Handle_NonTransactionalRequest_ShouldBypassTransaction()
    {
        // Arrange
        var behavior = new TransactionBehavior<NonTransactionalCommand, string>(
            _mockUnitOfWork.Object,
            _mockNonTransactionalLogger.Object);

        var request = new NonTransactionalCommand();
        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);
        _mockUnitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_ShouldExecuteWithinTransaction()
    {
        // Arrange
        var behavior = new TransactionBehavior<TransactionalCommand, string>(
            _mockUnitOfWork.Object,
            _mockTransactionalLogger.Object);

        var request = new TransactionalCommand();
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("TransactedSuccess");

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<string>>, CancellationToken>((action, ct) => action(ct));

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("TransactedSuccess", result);
        _mockUnitOfWork.Verify(
            u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_WhenExceptionOccurs_ShouldRethrowException()
    {
        // Arrange
        var behavior = new TransactionBehavior<TransactionalCommand, string>(
            _mockUnitOfWork.Object,
            _mockTransactionalLogger.Object);

        var request = new TransactionalCommand();
        var expectedException = new InvalidOperationException("DB Failure");
        RequestHandlerDelegate<string> next = (ct) => throw expectedException;

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<string>>, CancellationToken>((action, ct) => action(ct));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(request, next, CancellationToken.None));

        Assert.Equal("DB Failure", exception.Message);
    }
}
