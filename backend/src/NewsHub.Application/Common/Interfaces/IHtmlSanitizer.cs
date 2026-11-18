namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Abstraction làm sạch nội dung HTML rich-text nhằm chống tấn công Stored XSS.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>
    /// Làm sạch chuỗi HTML, loại bỏ các thẻ độc hại và script không an toàn.
    /// </summary>
    string Sanitize(string? html);
}
