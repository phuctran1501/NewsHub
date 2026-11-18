namespace NewsHub.Application.Common.Helpers;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// Tiện ích tính thời gian đọc ước tính của bài viết dựa trên số lượng từ trong nội dung (200 từ/phút).
/// </summary>
public static partial class ReadingTimeCalculator
{
    public const int WordsPerMinute = 200;

    /// <summary>
    /// Tính thời gian đọc (phút) từ nội dung văn bản thuần hoặc HTML.
    /// Tối thiểu là 1 phút.
    /// </summary>
    public static int Calculate(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 1;
        }

        // 1. Loại bỏ các thẻ HTML để lấy văn bản thuần
        var plainText = HtmlTagsRegex().Replace(content, " ");

        // 2. Tách từ theo khoảng trắng
        var words = plainText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        if (wordCount <= 0)
        {
            return 1;
        }

        // 3. 200 từ/phút, làm tròn lên
        var minutes = (int)Math.Ceiling((double)wordCount / WordsPerMinute);
        return Math.Max(1, minutes);
    }

    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagsRegex();
}
