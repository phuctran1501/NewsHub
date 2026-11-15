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
        // PNG full 8-byte signature: 89 50 4E 47 0D 0A 1A 0A
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;
        bytes[4] = 0x0D;
        bytes[5] = 0x0A;
        bytes[6] = 0x1A;
        bytes[7] = 0x0A;
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
    public void ValidateFile_MalformedPngHeader_MissingRemaining4Bytes_ThrowsAppValidationException()
    {
        // Chỉ có 4 bytes đầu 89 50 4E 47, nhưng 4 bytes sau là 0x00
        var bytes = new byte[100];
        bytes[0] = 0x89;
        bytes[1] = 0x50;
        bytes[2] = 0x4E;
        bytes[3] = 0x47;
        // bytes[4..7] remain 0x00
        using var stream = new MemoryStream(bytes);

        var ex = Assert.Throws<AppValidationException>(() =>
            CloudinaryService.ValidateFile(stream, "malformed.png"));

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
    public async Task UploadImageAsync_NonSeekableStream_ShouldBufferAndUploadSuccessfully()
    {
        // Arrange: Giả lập stream không hỗ trợ Seek (ví dụ NetworkStream, request body)
        var validPngBytes = CreateValidPngStream().ToArray();
        using var nonSeekableStream = new CustomNonSeekableStream(validPngBytes);
        const string fileName = "streamed_avatar.png";

        var uploadResult = new ImageUploadResult
        {
            PublicId = "newshub/avatars/streamed_user",
            SecureUrl = new Uri("https://res.cloudinary.com/newshub/image/upload/v1/newshub/avatars/streamed_user.webp"),
            Format = "webp",
            Bytes = validPngBytes.Length,
            Width = 400,
            Height = 400
        };

        _mockCloudinary
            .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        // Act
        var result = await _service.UploadImageAsync(nonSeekableStream, fileName, MediaFolder.Avatar);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newshub/avatars/streamed_user", result.PublicId);
        _mockCloudinary.Verify(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadImageAsync_NonSeekableStreamExceeding5MB_ThrowsAppValidationException()
    {
        // Arrange: Giả lập stream không seek được nhưng kích thước thực tế vượt quá 5MB
        var header = CreateValidPngStream(12).ToArray();
        using var nonSeekableOversizedStream = new CustomNonSeekableOversizedStream(CloudinaryService.MaxFileSizeBytes + 100, header);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            _service.UploadImageAsync(nonSeekableOversizedStream, "huge_stream.png", MediaFolder.ArticleCover));

        Assert.True(ex.Errors.ContainsKey("File"));
        Assert.Contains("5MB", ex.Errors["File"][0]);
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

    [Fact]
    public async Task DeleteImageAsync_WhenCancellationTokenAlreadyCanceled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.DeleteImageAsync("newshub/avatars/sample_to_delete", cts.Token));

        // SDK không bao giờ được gọi khi token đã bị hủy
        _mockCloudinary.Verify(c => c.DestroyAsync(It.IsAny<DeletionParams>()), Times.Never);
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

    /// <summary>
    /// Helper Stream mô phỏng NetworkStream không hỗ trợ Seek hoặc Length.
    /// </summary>
    private sealed class CustomNonSeekableStream : Stream
    {
        private readonly byte[] _data;
        private int _position;

        public CustomNonSeekableStream(byte[] data)
        {
            _data = data;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _data.Length)
            {
                return 0;
            }

            var bytesToCopy = Math.Min(count, _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, bytesToCopy);
            _position += bytesToCopy;
            return bytesToCopy;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// Helper Stream không hỗ trợ Seek mô phỏng nguồn dữ liệu quá khổ (> 5MB) phát sinh theo chunk.
    /// </summary>
    private sealed class CustomNonSeekableOversizedStream : Stream
    {
        private readonly long _totalSimulatedBytes;
        private readonly byte[] _header;
        private long _bytesEmitted;

        public CustomNonSeekableOversizedStream(long totalSimulatedBytes, byte[] header)
        {
            _totalSimulatedBytes = totalSimulatedBytes;
            _header = header;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytesEmitted >= _totalSimulatedBytes)
            {
                return 0;
            }

            var toRead = (int)Math.Min(count, _totalSimulatedBytes - _bytesEmitted);

            // Ghi header ở những bytes đầu tiên
            if (_bytesEmitted < _header.Length)
            {
                var headerToCopy = Math.Min(toRead, _header.Length - (int)_bytesEmitted);
                Array.Copy(_header, _bytesEmitted, buffer, offset, headerToCopy);
            }

            _bytesEmitted += toRead;
            return toRead;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
