using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Exceptions;

namespace NewsHub.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public record TestCommand(string Title) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Valid Title");

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNextAndReturnResponse()
    {
        // Arrange
        var mockValidator = new Mock<IValidator<TestCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("Valid Title");

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act
https://github.com/phuctran1501/NewsHub/pull/54/conflict?name=backend%252Ftests%252FNewsHub.Application.Tests%252FBehaviors%252FLoggingBehaviorTests.cs&base_oid=88dfc84fc6413ada48da9452c55d1e8eaf7446e5&head_oid=cfae72d260bfddbd25df97c7f5e5a3794e99a88b        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowAppValidationException_AndNotCallNext()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new("Title", "Title is required."),
            new("Title", "Title must be at least 5 characters.")
        };

        var mockValidator = new Mock<IValidator<TestCommand>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var validators = new[] { mockValidator.Object };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("");

        var nextCalled = false;
        RequestHandlerDelegate<string> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult("Success");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(command, next, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.True(exception.Errors.ContainsKey("Title"));
        Assert.Equal(2, exception.Errors["Title"].Length);
        Assert.Contains("Title is required.", exception.Errors["Title"]);
        Assert.Contains("Title must be at least 5 characters.", exception.Errors["Title"]);
    }
}
