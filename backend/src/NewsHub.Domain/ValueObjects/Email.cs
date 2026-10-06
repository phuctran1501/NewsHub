using System.Text.RegularExpressions;
using NewsHub.Domain.Exceptions;

namespace NewsHub.Domain.ValueObjects;

public readonly partial record struct Email
{
    public string Value { get; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException("Email", "Email không được để trống.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(normalized))
        {
            throw new ValidationException("Email", "Địa chỉ email không đúng định dạng.");
        }

        Value = normalized;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new(value);

    public override string ToString() => Value;
}
