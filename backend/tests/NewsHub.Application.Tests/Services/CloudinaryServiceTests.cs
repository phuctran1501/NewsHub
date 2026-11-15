using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Moq;
using NewsHub.Application.Common.Exceptions;
using NewsHub.Domain.Enums;
using NewsHub.Infrastructure.Services;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class CloudinaryServiceTests
{
    private readonly Mock<ICloudinary> _mockCloudinary;
    private readonly Mock<ILogger<CloudinaryService>> _mockLogger;
    private readonly CloudinaryService _service;

    public CloudinaryServiceTests()
    {
        _mockCloudinary = new Mock<ICloudinary>();
        _mockLogger = new Mock<ILogger<CloudinaryService>>();
        _service = new CloudinaryService(_mockCloudinary.Object, _mockLogger.Object);
    }

    private static MemoryStream CreateValidJpegStream(int length = 100)
    {
        var bytes = new byte[length];
        // JPEG magic bytes: FF D8 FF
        bytes[0] = 0xFF;
        bytes[1] = 0xD8;
        bytes[2] = 0xFF;
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateValidPngStream(int length = 100)
    {
        var bytes = new byte[length];
        // PNG magic bytes: 89 50 4E 47
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;
        return new MemoryStream(bytes);
    }

    private static MemoryStream CreateValidWebpStream(int length = 100)
    {
        var bytes = new byte[length];
        // WebP magic bytes: RIFF....WEBP
        bytes[0] = 0x52; // R
        bytes[1] = 0x49; // I
        bytes[2] = 0x46; // F
        bytes[3] = 0x46; // F
        bytes[8] = 0x57; // W
        bytes[9] = 0x45; // E
        bytes[10] = 0x42; // B
        bytes[11] = 0x50; // P
        return new MemoryStream(bytes);
    }

    [Fact]
    public void ValidateFile_NullOrEmptyStream_ThrowsAppValidationException()
    {
        using var emptyStream = new MemoryStream();

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(emptyStream, "test.png"));

        Assert.True(ex.Errors.ContainsKey("File"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateFile_EmptyFileName_ThrowsAppValidationException(string? fileName)
    {
        using var stream = CreateValidJpegStream();

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(stream, fileName!));

        Assert.True(ex.Errors.ContainsKey("FileName"));
    }

    [Theory]
    [InlineData("test.exe")]
    [InlineData("test.php")]
    [InlineData("test.pdf")]
    [InlineData("test.txt")]
    [InlineData("test")]
    public void ValidateFile_InvalidExtension_ThrowsAppValidationException(string fileName)
    {
        using var stream = CreateValidJpegStream();

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(stream, fileName));

        Assert.True(ex.Errors.ContainsKey("File"));
        Assert.Contains("Unsupported file extension", ex.Errors["File"][0]);
    }

    [Fact]
    public void ValidateFile_FileExceeds5MB_ThrowsAppValidationException()
    {
        // 5MB + 1 byte
        var largeStream = new Mock<MemoryStream> { CallBase = true };
        using var validHeaderStream = CreateValidJpegStream(12);

        // Tạo custom stream mô phỏng kích thước > 5MB
        using var oversizedStream = new CustomLengthStream(CloudinaryService.MaxFileSizeBytes + 1, validHeaderStream.ToArray());

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(oversizedStream, "large.jpg"));

        Assert.True(ex.Errors.ContainsKey("File"));
        Assert.Contains("5MB", ex.Errors["File"][0]);
    }

    [Fact]
    public void ValidateFile_InvalidMagicBytes_ThrowsAppValidationException()
    {
        // Giả lập file text đổi đuôi sang .png
        var textBytes = Encoding.UTF8.GetBytes("<html><body>Malicious Payload</body></html>");
        using var stream = new MemoryStream(textBytes);

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(stream, "fake.png"));

        Assert.True(ex.Errors.ContainsKey("File"));
        Assert.Contains("File signature is invalid", ex.Errors["File"][0]);
    }

    [Fact]
    public void ValidateFile_ValidImages_PassValidationWithoutException()
    {
        using var jpegStream = CreateValidJpegStream();
        using var pngStream = CreateValidPngStream();
        using var webpStream = CreateValidWebpStream();

        // Không ném exception
        CloudinaryService.ValidateFile(jpegStream, "photo.jpg");
        CloudinaryService.ValidateFile(pngStream, "icon.png");
        CloudinaryService.ValidateFile(webpStream, "banner.webp");

        // Đảm bảo stream position được reset về ban đầu
        Assert.Equal(0, jpegStream.Position);
        Assert.Equal(0, pngStream.Position);
        Assert.Equal(0, webpStream.Position);
    }

    [Fact]
    public async Task UploadImageAsync_ValidImage_ReturnsCloudinaryUploadResult()
    {
        // Arrange
        using var stream = CreateValidPngStream();
        const string fileName = "avatar.png";

        var uploadResult = new ImageUploadResult
        {
            PublicId = "newshub/avatars/user_123",
            SecureUrl = new Uri("https://res.cloudinary.com/newshub/image/upload/v1/newshub/avatars/user_123.webp"),
            Format = "webp",
            Bytes = 45000,
            Width = 500,
            Height = 500
        };

        _mockCloudinary
            .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        // Act
        var result = await _service.UploadImageAsync(stream, fileName, MediaFolder.Avatar);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newshub/avatars/user_123", result.PublicId);
        Assert.Equal("https://res.cloudinary.com/newshub/image/upload/v1/newshub/avatars/user_123.webp", result.Url);
        Assert.Equal("webp", result.Format);
        Assert.Equal(45000, result.Bytes);
        Assert.Equal(500, result.Width);
        Assert.Equal(500, result.Height);

        _mockCloudinary.Verify(c => c.UploadAsync(
            It.Is<ImageUploadParams>(p =>
                p.Folder == "newshub/avatars" &&
                p.Transformation != null &&
                p.File.FileName == fileName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_WhenCloudinaryReturnsError_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = CreateValidJpegStream();
        const string fileName = "article.jpg";

        var uploadResult = new ImageUploadResult
        {
            Error = new CloudinaryDotNet.Actions.Error
            {
                Message = "Invalid credentials or unauthorized"
            }
        };

        _mockCloudinary
            .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UploadImageAsync(stream, fileName, MediaFolder.ArticleCover));

        Assert.Contains("Invalid credentials or unauthorized", ex.Message);
    }

    [Theory]
    [InlineData("ok", true)]
    [InlineData("not found", true)]
    [InlineData("not Found", true)]
    [InlineData("error", false)]
    public async Task DeleteImageAsync_ReturnsExpectedResult_BasedOnCloudinaryResponse(string cloudinaryStatus, bool expected)
    {
        // Arrange
        const string publicId = "newshub/avatars/sample_123";

        var deletionResult = new DeletionResult
        {
            Result = cloudinaryStatus
        };

        _mockCloudinary
            .Setup(c => c.DestroyAsync(It.Is<DeletionParams>(p => p.PublicId == publicId)))
            .ReturnsAsync(deletionResult);

        // Act
        var result = await _service.DeleteImageAsync(publicId);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeleteImageAsync_WithEmptyPublicId_ReturnsFalseWithoutCallingApi(string? publicId)
    {
        // Act
        var result = await _service.DeleteImageAsync(publicId!);

        // Assert
        Assert.False(result);
        _mockCloudinary.Verify(c => c.DestroyAsync(It.IsAny<DeletionParams>()), Times.Never);
    }

    /// <summary>
    /// Helper Stream mô phỏng file dung lượng lớn mà không tốn RAM thật.
    /// </summary>
    private sealed class CustomLengthStream : MemoryStream
    {
        private readonly long _customLength;

        public CustomLengthStream(long customLength, byte[] initialHeader)
            : base(initialHeader)
        {
            _customLength = customLength;
        }

        public override long Length => _customLength;
    }
}
