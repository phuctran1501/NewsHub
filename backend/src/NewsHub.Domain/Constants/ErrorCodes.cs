namespace NewsHub.Domain.Constants;

/// <summary>
/// Danh mục tập hợp các mã lỗi hằng số chuỗi chuẩn hóa (Standard Error Codes) của hệ thống NewsHub.
/// Phục vụ đa ngôn ngữ (i18n) và chuẩn hóa xử lý lỗi phía Frontend React.
/// </summary>
public static class ErrorCodes
{
    // Validation (422)
    public const string ValidationFailed = "VALIDATION_FAILED";

    // Authentication (401, 409)
    public const string AuthInvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string AuthAccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AuthEmailAlreadyExists = "AUTH_EMAIL_ALREADY_EXISTS";

    // Token (401, 410)
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string TokenReuseDetected = "TOKEN_REUSE_DETECTED";

    // Authorization & Permissions (401, 403)
    public const string Unauthorized = "UNAUTHORIZED";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string Forbidden = "FORBIDDEN";
    public const string PressCardNotVerified = "PRESS_CARD_NOT_VERIFIED";
    public const string CannotDeletePublishedCompanyArticle = "CANNOT_DELETE_PUBLISHED_COMPANY_ARTICLE";

    // Resources (404)
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ArticleNotFound = "ARTICLE_NOT_FOUND";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string MediaNotFound = "MEDIA_NOT_FOUND";

    // Conflicts & Bad Requests (400, 409)
    public const string Conflict = "CONFLICT";
    public const string ArticleSlugConflict = "ARTICLE_SLUG_CONFLICT";
    public const string CannotFollowOwnCompany = "CANNOT_FOLLOW_OWN_COMPANY";
    public const string InvalidRequest = "INVALID_REQUEST";

    // Media (400, 422)
    public const string MediaInvalidFormat = "MEDIA_INVALID_FORMAT";
    public const string MediaSizeExceeded = "MEDIA_SIZE_EXCEEDED";
    public const string MediaUploadFailed = "MEDIA_UPLOAD_FAILED";

    // Server (500)
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
