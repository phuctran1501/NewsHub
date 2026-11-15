namespace NewsHub.Infrastructure.Services;

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Exceptions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public class CloudinaryService : ICloudinaryService
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly ICloudinary _cloudinary;
    private readonly ILogger<CloudinaryService>? _logger;

    public CloudinaryService(ICloudinary cloudinary, ILogger<CloudinaryService>? logger = null)
    {
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        MediaFolder folder,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(fileStream, fileName);

        var folderPath = GetFolderPath(folder);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folderPath,
            Transformation = new Transformation().FetchFormat("webp").Quality("auto"),
            Overwrite = false,
            UniqueFilename = true
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            _logger?.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {uploadResult.Error.Message}");
        }

        var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty;

        return new CloudinaryUploadResult(
            uploadResult.PublicId,
            url,
            uploadResult.Format ?? "webp",
            uploadResult.Bytes,
            uploadResult.Width,
            uploadResult.Height);
    }

    public async Task<bool> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return false;
        }

        var deletionParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deletionParams);

        if (result.Result != null && (result.Result.Equals("ok", StringComparison.OrdinalIgnoreCase) || result.Result.Equals("not found", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        _logger?.LogWarning("Cloudinary delete failed for publicId {PublicId}: {Result} - {Error}", publicId, result.Result, result.Error?.Message);
        return false;
    }

    public static void ValidateFile(Stream fileStream, string fileName)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new AppValidationException("File", "File stream cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new AppValidationException("FileName", "File name cannot be empty.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new AppValidationException("File", "Unsupported file extension. Allowed extensions are: .jpg, .jpeg, .png, .webp.");
        }

        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new AppValidationException("File", "File size exceeds the 5MB limit.");
        }

        // Validate Magic Bytes (File Header Signature)
        if (fileStream.CanSeek)
        {
            var initialPosition = fileStream.Position;
            try
            {
                var header = new byte[12];
                var bytesRead = fileStream.Read(header, 0, header.Length);
                if (bytesRead < 12 || !IsValidImageHeader(header))
                {
                    throw new AppValidationException("File", "File signature is invalid or does not match an allowed image type.");
                }
            }
            finally
            {
                fileStream.Position = initialPosition;
            }
        }
    }

    private static bool IsValidImageHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
        {
            return false;
        }

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return true;
        }

        // WebP: RIFF (bytes 0..3: 52 49 46 46) and WEBP (bytes 8..11: 57 45 42 50)
        if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return true;
        }

        return false;
    }

    private static string GetFolderPath(MediaFolder folder) => folder switch
    {
        MediaFolder.Avatar => "newshub/avatars",
        MediaFolder.ArticleCover => "newshub/article-covers",
        MediaFolder.ArticleBody => "newshub/article-body",
        _ => "newshub/others"
    };
}
