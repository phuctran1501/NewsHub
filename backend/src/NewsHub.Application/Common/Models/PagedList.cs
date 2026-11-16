namespace NewsHub.Application.Common.Models;

/// <summary>
/// Đại diện cho kết quả phân trang chuẩn mực ở tầng Application.
/// Tách biệt cấu trúc phân trang nghiệp vụ với Presentation API Envelope.
/// </summary>
/// <typeparam name="T">Kiểu dữ liệu của các phần tử trong trang.</typeparam>
public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount);
