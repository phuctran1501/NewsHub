namespace NewsHub.Infrastructure.Services;

using Ganss.Xss;
using NewsHub.Application.Common.Interfaces;

/// <summary>
/// Triển khai IHtmlSanitizer sử dụng thư viện Ganss.Xss.HtmlSanitizer.
/// Cấu hình whitelist thẻ và thuộc tính an toàn cho trình soạn thảo báo điện tử.
/// </summary>
public class HtmlSanitizerService : NewsHub.Application.Common.Interfaces.IHtmlSanitizer
{
    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        // Đảm bảo các thẻ bài viết rich-text phổ biến được cho phép
        _sanitizer.AllowedTags.Add("figure");
        _sanitizer.AllowedTags.Add("figcaption");
        _sanitizer.AllowedTags.Add("picture");
        _sanitizer.AllowedTags.Add("source");

        // Các thuộc tính cơ bản cho layout ảnh và responsive
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("style");
        _sanitizer.AllowedAttributes.Add("loading");
        _sanitizer.AllowedAttributes.Add("alt");
        _sanitizer.AllowedAttributes.Add("title");
        _sanitizer.AllowedAttributes.Add("srcset");
        _sanitizer.AllowedAttributes.Add("sizes");

        // Triệt tiêu các giao thức nguy hiểm
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return _sanitizer.Sanitize(html);
    }
}
