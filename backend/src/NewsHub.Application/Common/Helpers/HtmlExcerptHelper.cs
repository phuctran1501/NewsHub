namespace NewsHub.Application.Common.Helpers;

using System.Text.RegularExpressions;

public static class HtmlExcerptHelper
{
    private static readonly Regex HtmlTagsRegex = new("<[^>]+>", RegexOptions.Compiled);

    public static string? ExtractExcerpt(string? html, int maxLength = 160)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var plainText = HtmlTagsRegex.Replace(html, " ").Trim();
        plainText = Regex.Replace(plainText, @"\s+", " ");

        if (plainText.Length <= maxLength)
        {
            return plainText;
        }

        return plainText[..maxLength].Trim() + "...";
    }
}
