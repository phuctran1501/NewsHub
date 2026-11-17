namespace NewsHub.Application.Features.Categories.Constants;

public static class CategoryCacheKeys
{
    /// <summary>
    /// Cache key lưu trữ toàn bộ danh mục tin tức kèm số lượng bài viết (TTL 24 giờ).
    /// </summary>
    public const string AllCategories = "categories:all";
}
