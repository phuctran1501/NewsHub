namespace NewsHub.Application.Common.Interfaces;

using NewsHub.Application.Common.Models;
using NewsHub.Domain.Enums;

public interface ICloudinaryService
{
    /// <summary>
    /// Tải tệp tin ảnh từ Stream lên Cloudinary, tự động chuyển đổi sang WebP và tối ưu chất lượng.
    /// </summary>
    Task<CloudinaryUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        MediaFolder folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa tệp tin ảnh từ xa trên máy chủ Cloudinary thông qua PublicId.
    /// </summary>
    Task<bool> DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);
}
