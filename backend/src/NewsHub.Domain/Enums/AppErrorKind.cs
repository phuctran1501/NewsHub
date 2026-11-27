namespace NewsHub.Domain.Enums;

/// <summary>
/// Phân loại 7 nhóm mã lỗi HTTP chuẩn mực trong hệ thống NewsHub.
/// Được gắn kèm trong AppException để tầng Middleware tự động ánh xạ đúng HTTP Status Code.
/// </summary>
public enum AppErrorKind
{
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    Gone = 410,
    Validation = 422,
    ServiceUnavailable = 503
}
