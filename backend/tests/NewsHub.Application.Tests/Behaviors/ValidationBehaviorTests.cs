using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NewsHub.Application.Common.Behaviors;
using NewsHub.Application.Common.Exceptions;
using Xunit;

namespace NewsHub.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public record SampleCommand(string Title) : IRequest<string>;

    public class SampleCommandValidator : AbstractValidator<SampleCommand>
    {
        public SampleCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống.")
                .MinimumLength(5).WithMessage("Tiêu đề phải có ít nhất 5 ký tự.");
        }
    }

    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ShouldProceedToNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<SampleCommand, string>(Enumerable.Empty<IValidator<SampleCommand>>());
        var command = new SampleCommand("Valid Title");
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldProceedToNext()
    {
        // Arrange
        var validators = new List<IValidator<SampleCommand>> { new SampleCommandValidator() };
        var behavior = new ValidationBehavior<SampleCommand, string>(validators);
        var command = new SampleCommand("Valid Title");
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowAppValidationException()
    {
        // Arrange
        var validators = new List<IValidator<SampleCommand>> { new SampleCommandValidator() };
        var behavior = new ValidationBehavior<SampleCommand, string>(validators);
        var command = new SampleCommand(""); // invalid
        RequestHandlerDelegate<string> next = (ct) => Task.FromResult("Success");

        // Act
        var act = () => behavior.Handle(command, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppValidationException>();
        exception.Which.Errors.Should().ContainKey("Title");
        exception.Which.Errors["Title"].Should().Contain("Tiêu đề không được để trống.");
    }
}
