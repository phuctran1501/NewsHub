using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

namespace NewsHub.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("0381234567", "+84381234567")] // Viettel 03x
    [InlineData("0861234567", "+84861234567")] // Viettel 086
    [InlineData("0981234567", "+84981234567")] // Viettel 098
    [InlineData("0701234567", "+84701234567")] // MobiFone 070
    [InlineData("0791234567", "+84791234567")] // MobiFone 079
    [InlineData("0891234567", "+84891234567")] // MobiFone 089
    [InlineData("0901234567", "+84901234567")] // MobiFone 090
    [InlineData("0811234567", "+84811234567")] // VinaPhone 081
    [InlineData("0881234567", "+84881234567")] // VinaPhone 088
    [InlineData("0911234567", "+84911234567")] // VinaPhone 091
    [InlineData("0521234567", "+84521234567")] // Vietnamobile 052
    [InlineData("0921234567", "+84921234567")] // Vietnamobile 092
    [InlineData("0591234567", "+84591234567")] // Gmobile 059
    [InlineData("0991234567", "+84991234567")] // Gmobile 099
    [InlineData("0871234567", "+84871234567")] // Itelecom 087
    [InlineData("0551234567", "+84551234567")] // Wintel 055
    public void Constructor_WithValidDomesticCarriers_ShouldNormalizeToE164(string rawPhone, string expected)
    {
        var phone = new PhoneNumber(rawPhone);
        Assert.Equal(expected, phone.Value);
        Assert.Equal(expected, phone.ToString());
    }

    [Theory]
    [InlineData("098.765.4321", "+84987654321")] // Dots
    [InlineData("098-765-4321", "+84987654321")] // Hyphens
    [InlineData("098 765 4321", "+84987654321")] // Spaces
    [InlineData("(098) 765-4321", "+84987654321")] // Parentheses & hyphen
    [InlineData("+84 987 654 321", "+84987654321")] // International with spaces
    public void Constructor_WithSeparators_ShouldCleanAndNormalize(string rawPhone, string expected)
    {
        var phone = new PhoneNumber(rawPhone);
        Assert.Equal(expected, phone.Value);
    }

    [Theory]
    [InlineData("+84987654321", "+84987654321")]
    [InlineData("84987654321", "+84987654321")]
    public void Constructor_WithInternationalPrefix_ShouldNormalizeToE164(string rawPhone, string expected)
    {
        var phone = new PhoneNumber(rawPhone);
        Assert.Equal(expected, phone.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0123456789")] // Old 11-digit or invalid 01x prefix
    [InlineData("02412345678")] // Hanoi Landline 024
    [InlineData("0412345678")] // Invalid prefix
    [InlineData("0612345678")] // Invalid prefix
    [InlineData("091234567")] // Too short (9 digits)
    [InlineData("09123456789")] // Too long (11 digits)
    [InlineData("098abc4321")] // Containing letters
    [InlineData("091\u0662\u0663\u0664\u0665\u0666\u06678")] // Unicode Arabic digits
    [InlineData("invalid")]
    public void Constructor_WithInvalidPhone_ShouldThrowValidationException(string invalidPhone)
    {
        var ex = Assert.Throws<ValidationException>(() => new PhoneNumber(invalidPhone));
        Assert.Contains("PhoneNumber", ex.Errors.Keys);
    }

    [Theory]
    [InlineData("0912345678", true)]
    [InlineData("+84912345678", true)]
    [InlineData("0123456789", false)]
    [InlineData("invalid", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsValid_ShouldReturnCorrectStatus(string? phone, bool expected)
    {
        Assert.Equal(expected, PhoneNumber.IsValid(phone));
    }

    [Fact]
    public void ImplicitConversion_ToStringAndFromPhoneNumber_ShouldWorkCorrectly()
    {
        PhoneNumber phone = "0987654321";
        string stringValue = phone;

        Assert.Equal("+84987654321", stringValue);
        Assert.Equal("+84987654321", phone.Value);
    }
}
