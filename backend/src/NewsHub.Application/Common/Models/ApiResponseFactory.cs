namespace NewsHub.Application.Common.Models;

using NewsHub.Domain.Exceptions;

/// <summary>
/// Static Factory đóng gói dữ liệu phản hồi API thành Envelope chuẩn mực:
/// - Success: { "data": T, "meta": {}, "correlationId": string }
/// - Paged: { "data": T[], "meta": PagedMeta, "correlationId": string }
/// Tự động kiểm tra tính hợp lệ của tham số phân trang (pageNumber > 0, pageSize > 0).
/// </summary>
public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T data, string correlationId, object? meta = null)
        => new(data, meta ?? new object(), correlationId);

    public static PagedApiResponse<T> Paged<T>(
        IEnumerable<T> data,
        int pageNumber,
        int pageSize,
        int totalCount,
        string correlationId)
    {
        if (pageNumber <= 0)
        {
            throw new BadRequestException("pageNumber must be greater than 0.");
        }

        if (pageSize <= 0)
        {
            throw new BadRequestException("pageSize must be greater than 0.");
        }

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var meta = new PagedMeta(
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);

        return new PagedApiResponse<T>(data, meta, correlationId);
    }

    public static PagedApiResponse<T> Paged<T>(
        PagedList<T> pagedList,
        string correlationId)
        => Paged(pagedList.Items, pagedList.PageNumber, pagedList.PageSize, pagedList.TotalCount, correlationId);
}
