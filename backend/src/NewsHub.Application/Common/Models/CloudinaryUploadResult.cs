namespace NewsHub.Application.Common.Models;

public sealed record CloudinaryUploadResult(
    string PublicId,
    string Url,
    string Format,
    long Bytes,
    int Width,
    int Height);
