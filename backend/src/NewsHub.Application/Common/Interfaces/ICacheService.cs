namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ đệm phân tán và kiểm soát tương tranh.
/// Hỗ trợ mẫu Cache-Aside, tự động hủy theo tiền tố và Khóa phân tán.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Lấy dữ liệu từ cache theo khóa. Trả về null nếu không tìm thấy hoặc Redis offline.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu dữ liệu vào cache kèm thời gian hết hạn (TTL).
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mẫu Cache-Aside: Lấy dữ liệu từ cache nếu có; nếu không, gọi factory để lấy dữ liệu gốc và nạp vào cache.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa một khóa cụ thể khỏi cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa hàng loạt khóa khớp với tiền tố (Prefix Invalidation).
    /// </summary>
    Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra xem khóa có tồn tại trong cache không.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Chiếm khóa phân tán (Distributed Lock). 
    /// Trả về IDisposable để tự động giải phóng khi kết thúc scope; trả về null nếu không chiếm được khóa.
    /// </summary>
    /// <param name="key">Tên tài nguyên cần khóa.</param>
    /// <param name="expiration">Thời gian giữ khóa tối đa trước khi tự hủy (chống deadlock).</param>
    /// <param name="timeout">Thời gian tối đa chờ để chiếm khóa (mặc định thử 1 lần không chờ).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    Task<IDisposable?> AcquireLockAsync(
        string key,
        TimeSpan expiration,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}

