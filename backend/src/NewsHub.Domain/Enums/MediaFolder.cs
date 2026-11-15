namespace NewsHub.Domain.Enums;

public enum MediaFolder
{
    /// <summary>
    /// Thư mục chứa ảnh đại diện cá nhân của người dùng (avatars).
    /// </summary>
    Avatar = 1,

    /// <summary>
    /// Thư mục chứa ảnh bìa tiêu điểm của bài viết (article-covers).
    /// </summary>
    ArticleCover = 2,

    /// <summary>
    /// Thư mục chứa ảnh minh họa chèn trong nội dung rich-text của bài báo (article-body).
    /// </summary>
    ArticleBody = 3
}
