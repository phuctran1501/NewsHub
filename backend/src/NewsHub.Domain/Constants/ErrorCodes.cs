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
    public const string CannotLockOwnAccount = "CANNOT_LOCK_OWN_ACCOUNT";

    // Resources (404)
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string ArticleNotFound = "ARTICLE_NOT_FOUND";
    public const string ArticleNotPublished = "ARTICLE_NOT_PUBLISHED";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string MediaNotFound = "MEDIA_NOT_FOUND";
    public const string BookmarkNotFound = "BOOKMARK_NOT_FOUND";
    public const string ReadingHistoryNotFound = "READING_HISTORY_NOT_FOUND";
    public const string ReportNotFound = "REPORT_NOT_FOUND";
    public const string CompanyNotFound = "COMPANY_NOT_FOUND";
    public const string RequestNotFound = "REQUEST_NOT_FOUND";
    public const string NotificationNotFound = "NOTIFICATION_NOT_FOUND";

    // Conflicts & Bad Requests (400, 409, 429)
    public const string Conflict = "CONFLICT";
    public const string CategoryConflict = "CATEGORY_CONFLICT";
    public const string CategoryInUse = "CATEGORY_IN_USE";
    public const string ArticleSlugConflict = "ARTICLE_SLUG_CONFLICT";
    public const string BookmarkAlreadyExists = "BOOKMARK_ALREADY_EXISTS";
    public const string CompanyAlreadyExists = "COMPANY_ALREADY_EXISTS";
    public const string InvalidCompanyStatus = "INVALID_COMPANY_STATUS";
    public const string MaxCompaniesLimitReached = "MAX_COMPANIES_LIMIT_REACHED";
    public const string UserIsNotJournalist = "USER_IS_NOT_A_JOURNALIST";
    public const string AlreadyCompanyMember = "ALREADY_COMPANY_MEMBER";
    public const string NotCompanyMember = "NOT_COMPANY_MEMBER";
    public const string OnlyOwnerAllowed = "ONLY_OWNER_ALLOWED";
    public const string CannotLeaveAsOwner = "CANNOT_LEAVE_AS_OWNER";
    public const string CannotDissolveWithMembers = "CANNOT_DISSOLVE_COMPANY_WITH_MEMBERS";
    public const string CannotRemoveSelf = "CANNOT_REMOVE_SELF";
    public const string CannotRemoveOwner = "CANNOT_REMOVE_OWNER";
    public const string InvitationAlreadyPending = "INVITATION_ALREADY_PENDING";
    public const string JoinRequestAlreadyPending = "JOIN_REQUEST_ALREADY_PENDING";
    public const string InvalidRequestStatus = "INVALID_REQUEST_STATUS";
    public const string CannotFollowOwnCompany = "CANNOT_FOLLOW_OWN_COMPANY";
    public const string CannotUnfollowOwnCompany = "CANNOT_UNFOLLOW_OWN_COMPANY";
    public const string ArticleHasNoCompany = "ARTICLE_HAS_NO_COMPANY";
    public const string ArticleNotSubmittedToCompany = "ARTICLE_NOT_SUBMITTED_TO_COMPANY";
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
    public const string ReportAlreadySubmitted = "REPORT_ALREADY_SUBMITTED";

    // Media (400, 409, 422)
    public const string MediaInvalidFormat = "MEDIA_INVALID_FORMAT";
    public const string MediaSizeExceeded = "MEDIA_SIZE_EXCEEDED";
    public const string MediaUploadFailed = "MEDIA_UPLOAD_FAILED";
    public const string MediaDeleteFailed = "MEDIA_DELETE_FAILED";
    public const string MediaInUse = "MEDIA_IN_USE";

    // AI Services (400, 404, 502, 503)
    public const string AiServiceUnavailable = "AI_SERVICE_UNAVAILABLE";
    public const string AiSummaryNotFound = "AI_SUMMARY_NOT_FOUND";
    public const string InvalidAiResponse = "INVALID_AI_RESPONSE";

    // Server (500)
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
