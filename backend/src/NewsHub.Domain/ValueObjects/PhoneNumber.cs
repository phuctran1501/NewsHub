using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using NewsHub.Domain.Exceptions;

namespace NewsHub.Domain.ValueObjects;

/// <summary>
/// Value Object chuẩn hóa số điện thoại di động các nhà mạng Việt Nam theo chuẩn quốc tế E.164 (+84).
/// Hỗ trợ tất cả đầu số 10 chữ số hiện hành: 03x, 05x, 07x, 08x, 09x (Viettel, VinaPhone, MobiFone, Vietnamobile, Gmobile, Itelecom, Wintel).
/// Tự động làm sạch dấu phân cách (dấu chấm, gạch nối, khoảng trắng, ngoặc đơn).
/// </summary>
public readonly partial record struct PhoneNumber
{
    public string Value { get; }

    [GeneratedRegex(@"^(?:\+84|84|0)([35789][0-9]{8})$")]
    private static partial Regex VietnamPhoneRegex();

    [GeneratedRegex(@"[\s\.\-\(\)]")]
    private static partial Regex PhoneSeparatorRegex();

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException("PhoneNumber", "Số điện thoại không được để trống.");
        }

        if (!TryNormalize(value, out var normalized))
        {
            throw new ValidationException("PhoneNumber", "Số điện thoại không đúng định dạng nhà mạng Việt Nam (+84).");
        }

        Value = normalized;
    }

    public static bool TryNormalize(string? raw, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var cleaned = PhoneSeparatorRegex().Replace(raw.Trim(), "");
        var match = VietnamPhoneRegex().Match(cleaned);

        if (!match.Success)
        {
            return false;
        }

        // match.Groups[1] chứa 9 chữ số thuê bao (bắt đầu bằng 3, 5, 7, 8 hoặc 9)
        normalized = $"+84{match.Groups[1].Value}";
        return true;
    }

    public static bool IsValid(string? raw) => TryNormalize(raw, out _);

    public static string Normalize(string raw)
    {
        if (!TryNormalize(raw, out var normalized))
        {
            throw new ValidationException("PhoneNumber", "Số điện thoại không đúng định dạng nhà mạng Việt Nam (+84).");
        }

        return normalized;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static implicit operator PhoneNumber(string value) => new(value);

    public override string ToString() => Value;
}
