namespace NewsHub.Domain.Common;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Tiện ích chuẩn hóa và tạo SEO Slug thân thiện URL từ chuỗi văn bản tiếng Việt hoặc quốc tế.
/// </summary>
public static partial class SlugHelper
{
    private const int MaxSlugLength = 120;

    /// <summary>
    /// Chuyển đổi chuỗi văn bản tiếng Việt có dấu thành URL Slug dạng kebab-case.
    /// Ví dụ: "Thời sự & Đời sống!" -> "thoi-su-doi-song".
    /// </summary>
    /// <param name="text">Chuỗi văn bản nguồn.</param>
    /// <param name="maxLength">Độ dài tối đa của slug (mặc định 120 ký tự).</param>
    /// <returns>Chuỗi slug chuẩn hóa hoặc string.Empty nếu chuỗi nguồn rỗng.</returns>
    public static string GenerateSlug(string? text, int maxLength = MaxSlugLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (maxLength <= 0)
        {
            maxLength = MaxSlugLength;
        }

        // 1. Chuyển đổi chữ đ/Đ riêng biệt trước khi normalize FormD
        var preprocessed = text.Trim()
            .Replace("đ", "d", StringComparison.OrdinalIgnoreCase)
            .Replace("Đ", "d", StringComparison.OrdinalIgnoreCase);

        // 2. Tách dấu thanh tiếng Việt qua Unicode Normalization FormD
        var normalizedFormD = preprocessed.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var ch in normalizedFormD)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(ch);
            }
        }

        // 3. Chuẩn hóa về FormC và chuyển thành chữ thường
        var cleanText = stringBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        // 4. Thay thế ký tự không phải chữ số [a-z0-9] thành dấu gạch ngang
        cleanText = NonAlphanumericRegex().Replace(cleanText, "-");

        // 5. Gộp nhiều dấu gạch ngang liên tiếp thành 1 dấu gạch ngang duy nhất
        cleanText = MultipleHyphensRegex().Replace(cleanText, "-");

        // 6. Cắt bỏ dấu gạch ngang ở đầu và cuối chuỗi
        cleanText = cleanText.Trim('-');

        // 7. Giới hạn độ dài tối đa
        if (cleanText.Length > maxLength)
        {
            cleanText = cleanText[..maxLength].TrimEnd('-');
        }

        return cleanText;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"[\s-]+", RegexOptions.Compiled)]
    private static partial Regex MultipleHyphensRegex();
}
