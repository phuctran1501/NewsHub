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
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AuthAccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AuthEmailAlreadyExists = "AUTH_EMAIL_ALREADY_EXISTS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string AuthInvalidCurrentPassword = "AUTH_INVALID_CURRENT_PASSWORD";

    // Token (401, 410)
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string TokenInvalid = "TOKEN_INVALID";
    public const string TokenRevoked = "TOKEN_REVOKED";
    public const string TokenReuseDetected = "TOKEN_REUSE_DETECTED";
    public const string RefreshTokenRequired = "REFRESH_TOKEN_REQUIRED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string InvalidPreviewToken = "INVALID_PREVIEW_TOKEN";

    // Authorization & Permissions (401, 403)
    public const string Unauthorized = "UNAUTHORIZED";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string Forbidden = "FORBIDDEN";
    public const string PressCardNotVerified = "PRESS_CARD_NOT_VERIFIED";
    public const string ArticleForbiddenEdit = "ARTICLE_FORBIDDEN_EDIT";
    public const string CannotDeletePublishedCompanyArticle = "CANNOT_DELETE_PUBLISHED_COMPANY_ARTICLE";

    // Resources (404)
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ArticleNotFound = "ARTICLE_NOT_FOUND";
    public const string ArticleNotPublished = "ARTICLE_NOT_PUBLISHED";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string MediaNotFound = "MEDIA_NOT_FOUND";

    // Conflicts & Bad Requests (400, 409, 429)
    public const string Conflict = "CONFLICT";
    public const string CategoryConflict = "CATEGORY_CONFLICT";
    public const string CategoryInUse = "CATEGORY_IN_USE";
    public const string ArticleSlugConflict = "ARTICLE_SLUG_CONFLICT";
    public const string CannotFollowOwnCompany = "CANNOT_FOLLOW_OWN_COMPANY";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string ReportAlreadySubmitted = "REPORT_ALREADY_SUBMITTED";

    // Media (400, 409, 422)
    public const string MediaInvalidFormat = "MEDIA_INVALID_FORMAT";
    public const string MediaSizeExceeded = "MEDIA_SIZE_EXCEEDED";
    public const string MediaUploadFailed = "MEDIA_UPLOAD_FAILED";
    public const string MediaDeleteFailed = "MEDIA_DELETE_FAILED";
    public const string MediaInUse = "MEDIA_IN_USE";

    // Server (500)
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
