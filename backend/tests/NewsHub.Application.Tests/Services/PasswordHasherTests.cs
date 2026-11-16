namespace NewsHub.Application.Tests.Services;

using NewsHub.Infrastructure.Services;
using Xunit;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnValidSaltAndHashFormat()
    {
        var password = "SecurePassword123!";
        var hash = _hasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        var parts = hash.Split(':');
        Assert.Equal(4, parts.Length);

        // Hash is 32 bytes = 64 hex chars, Salt is 16 bytes = 32 hex chars
        Assert.Equal(64, parts[0].Length);
        Assert.Equal(32, parts[1].Length);
        Assert.Equal("100000", parts[2]);
        Assert.Equal("SHA512", parts[3]);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        var password = "CorrectPassword123!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyPassword("WrongPassword123!", hash);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid_no_colon")]
    [InlineData("not_hex:invalid_hex")]
    [InlineData("short:short")]
    public void VerifyPassword_WithMalformedHash_ShouldReturnFalse(string malformedHash)
    {
        var isValid = _hasher.VerifyPassword("Password123!", malformedHash);
        Assert.False(isValid);
    }

    [Fact]
    public void HashPassword_SamePasswordMultipleTimes_ShouldProduceDifferentHashesDueToSalt()
    {
        var password = "SamePassword123!";
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.VerifyPassword(password, hash1));
        Assert.True(_hasher.VerifyPassword(password, hash2));
    }
}
