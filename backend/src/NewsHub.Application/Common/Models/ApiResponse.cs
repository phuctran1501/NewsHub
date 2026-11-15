namespace NewsHub.Application.Common.Models;

/// <summary>
/// Envelope chuẩn cho phản hồi API thành công với dữ liệu đơn lẻ.
/// Định dạng: { "data": T, "meta": {}, "correlationId": string }
/// </summary>
public sealed record ApiResponse<T>(
    T Data,
    object Meta,
    string CorrelationId);

/// <summary>
/// Thông tin metadata phân trang chuẩn cho các danh sách dữ liệu.
/// </summary>
public sealed record PagedMeta(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

/// <summary>
/// Envelope chuẩn cho phản hồi API thành công với dữ liệu phân trang.
/// Định dạng: { "data": T[], "meta": PagedMeta, "correlationId": string }
/// </summary>
public sealed record PagedApiResponse<T>(
    IEnumerable<T> Data,
    PagedMeta Meta,
    string CorrelationId);

/// <summary>
/// Envelope chuẩn cho phản hồi API khi xảy ra lỗi.
/// Định dạng: { "error": { "code", "message", "details" }, "correlationId": string }
/// </summary>
public sealed record ApiErrorResponse(
    ApiErrorDetail Error,
    string CorrelationId);

/// <summary>
/// Chi tiết lỗi gồm mã lỗi định danh, thông điệp người dùng và danh sách lỗi trường dữ liệu.
/// </summary>
public sealed record ApiErrorDetail(
    string Code,
    string Message,
    IReadOnlyList<ApiValidationErrorItem> Details);

/// <summary>
/// Lỗi chi tiết cho từng trường dữ liệu vi phạm (dùng cho Form Validation 422).
/// </summary>
public sealed record ApiValidationErrorItem(
    string Field,
    string Message);
