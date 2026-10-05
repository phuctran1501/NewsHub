using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

namespace NewsHub.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com", "test@example.com")]
    [InlineData("USER@DOMAIN.COM", "user@domain.com")]
    [InlineData("  hello@newshub.io  ", "hello@newshub.io")]
    [InlineData("journalist.news+tag@company.co.uk", "journalist.news+tag@company.co.uk")]
    public void Constructor_WithValidEmail_ShouldNormalizeAndSucceed(string input, string expected)
    {
        // Act
        var email = new Email(input);

        // Assert
        Assert.Equal(expected, email.Value);
        Assert.Equal(expected, email.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespace_ShouldThrowValidationException(string? invalidInput)
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => new Email(invalidInput!));
        Assert.Contains("Email", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@missingusername.com")]
    [InlineData("username@.com")]
    [InlineData("username@domain")]
    public void Constructor_WithInvalidFormat_ShouldThrowValidationException(string invalidFormat)
    {
        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => new Email(invalidFormat));
        Assert.Contains("Email", exception.Errors.Keys);
    }

    [Fact]
    public void ImplicitConversion_ToStringAndFromEmail_ShouldWorkSeamlessly()
    {
        // Arrange
        Email email = new("user@newshub.com");

        // Act
        string stringValue = email;
        Email roundTrip = (Email)"user@newshub.com";

        // Assert
        Assert.Equal("user@newshub.com", stringValue);
        Assert.Equal(email, roundTrip);
    }
}
