namespace NewsHub.Domain.Enums;

/// <summary>
/// Phân loại mục đích tạo vector nhúng bất đối xứng theo chuẩn Google Gemini Embedding API.
/// </summary>
public enum EmbeddingTaskType
{
    /// <summary>
    /// Dùng khi lưu trữ / lập chỉ mục tài liệu hoặc bài báo vào cơ sở dữ liệu vector (RETRIEVAL_DOCUMENT).
    /// </summary>
    RetrievalDocument = 0,

    /// <summary>
    /// Dùng khi nhúng câu hỏi hoặc cụm từ tìm kiếm của người dùng (RETRIEVAL_QUERY).
    /// </summary>
    RetrievalQuery = 1
}
