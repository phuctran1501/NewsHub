using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Interfaces;
using NSubstitute;
using Xunit;

namespace NewsHub.Application.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<NonTransactionalCommand, string>> _nonTransactionalLogger;
    private readonly ILogger<TransactionBehavior<TransactionalCommand, string>> _transactionalLogger;

    public TransactionBehaviorTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _nonTransactionalLogger = Substitute.For<ILogger<TransactionBehavior<NonTransactionalCommand, string>>>();
        _transactionalLogger = Substitute.For<ILogger<TransactionBehavior<TransactionalCommand, string>>>();
    }

    public record NonTransactionalCommand : IRequest<string>;
    public record TransactionalCommand : IRequest<string>, ITransactionalRequest;

    [Fact]
    public async Task Handle_NonTransactionalRequest_ShouldBypassTransaction()
    {
        // Arrange
        var behavior = new TransactionBehavior<NonTransactionalCommand, string>(
            _unitOfWork,
            _nonTransactionalLogger);

        var request = new NonTransactionalCommand();
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
        await _unitOfWork.DidNotReceiveWithAnyArgs()
            .ExecuteInTransactionAsync<string>(default!, default);
    }

    [Fact]
    public async Task Handle_TransactionalRequest_ShouldExecuteWithinTransaction()
    {
        // Arrange
        var behavior = new TransactionBehavior<TransactionalCommand, string>(
            _unitOfWork,
            _transactionalLogger);

        var request = new TransactionalCommand();
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("TransactedSuccess");

        _unitOfWork.ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var action = callInfo.Arg<Func<CancellationToken, Task<string>>>();
                var ct = callInfo.Arg<CancellationToken>();
                return action(ct);
            });

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("TransactedSuccess");
        await _unitOfWork.Received(1).ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TransactionalRequest_WhenExceptionOccurs_ShouldRethrowException()
    {
        // Arrange
        var behavior = new TransactionBehavior<TransactionalCommand, string>(
            _unitOfWork,
            _transactionalLogger);

        var request = new TransactionalCommand();
        var expectedException = new InvalidOperationException("DB Failure");
        RequestHandlerDelegate<string> next = (ct) => throw expectedException;

        _unitOfWork.ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<string>>>(),
            Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var action = callInfo.Arg<Func<CancellationToken, Task<string>>>();
                var ct = callInfo.Arg<CancellationToken>();
                return await action(ct);
            });

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB Failure");
    }
}
