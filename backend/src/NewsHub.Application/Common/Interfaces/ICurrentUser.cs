namespace NewsHub.Application.Common.Interfaces;

using NewsHub.Domain.Enums;

/// <summary>
/// Abstraction an toàn trích xuất thông tin người dùng hiện tại từ ngữ cảnh request (JWT Claims).
/// Phòng chống lỗ hổng IDOR (Insecure Direct Object Reference) bằng cách không tin cậy userId từ client.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// ID của người dùng đăng nhập trích xuất từ JWT token (ClaimTypes.NameIdentifier / sub / id).
    /// Trả về null nếu người dùng chưa xác thực (Guest).
    /// </summary>
    Guid? Id { get; }

    /// <summary>
    /// Email của người dùng (ClaimTypes.Email / email).
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Vai trò của người dùng (User, Journalist, Admin).
    /// </summary>
    UserRole? Role { get; }

    /// <summary>
    /// Kiểm tra người dùng đã đăng nhập hợp lệ hay chưa.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Kiểm tra người dùng có thuộc vai trò chỉ định hay không.
    /// </summary>
    bool IsInRole(UserRole role);
}
