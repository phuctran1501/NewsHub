namespace NewsHub.Application.Tests.Features.Media.Commands;

using System.Text;
using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Media.Commands.UploadMediaImage;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class UploadMediaImageCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly UploadMediaImageCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUploadToCloudinaryAndSaveMediaFile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var uploadResult = new CloudinaryUploadResult(
            "folder/test_public_id",
            "https://res.cloudinary.com/demo/image/upload/v1234/folder/test_public_id.webp",
            "webp",
            1024,
            800,
            600);

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(
                It.IsAny<Stream>(),
                "avatar.png",
                MediaFolder.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var mediaFiles = new List<MediaFile>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaFiles.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new UploadMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));
        var command = new UploadMediaImageCommand(
            stream,
            "avatar.png",
            stream.Length,
            "image/png",
            MediaFolder.Avatar);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(uploadResult.PublicId, result.PublicId);
        Assert.Equal(uploadResult.Url, result.Url);
        Assert.Equal("avatar.png", result.OriginalFileName);
        Assert.Equal("webp", result.Format);
        Assert.Equal(1024, result.Bytes);
        Assert.Equal(800, result.Width);
        Assert.Equal(600, result.Height);
        Assert.Equal(MediaFolder.Avatar, result.Folder);
        Assert.Equal(userId, result.UploadedByUserId);

        _dbContextMock.Verify(db => db.MediaFiles.Add(It.Is<MediaFile>(m =>
            m.PublicId == uploadResult.PublicId &&
            m.UploadedByUserId == userId &&
            m.Folder == MediaFolder.Avatar)), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDatabaseSaveFails_ShouldInvokeCompensatingDeleteOnCloudinaryAndRethrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var uploadResult = new CloudinaryUploadResult(
            "folder/compensate_public_id",
            "https://res.cloudinary.com/demo/image/upload/v1234/folder/compensate_public_id.webp",
            "webp",
            2048,
            800,
            600);

        _cloudinaryMock
            .Setup(c => c.UploadImageAsync(
                It.IsAny<Stream>(),
                "test.png",
                MediaFolder.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var mediaFiles = new List<MediaFile>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaFiles.Object);
        _dbContextMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var handler = new UploadMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));
        var command = new UploadMediaImageCommand(
            stream,
            "test.png",
            stream.Length,
            "image/png",
            MediaFolder.Avatar);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        // Verify Compensating action ran with CancellationToken.None
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(uploadResult.PublicId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);

        var handler = new UploadMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-image-bytes"));
        var command = new UploadMediaImageCommand(
            stream,
            "cover.jpg",
            stream.Length,
            "image/jpeg",
            MediaFolder.ArticleCover);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Equal(ErrorCodes.Unauthorized, exception.ErrorCode);
    }

    [Theory]
    [InlineData("avatar.jpg")]
    [InlineData("avatar.jpeg")]
    [InlineData("photo.png")]
    [InlineData("image.webp")]
    public void Validator_WithValidExtensions_ShouldNotHaveValidationError(string fileName)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaImageCommand(stream, fileName, 100, "image/jpeg", MediaFolder.Avatar);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("document.pdf")]
    [InlineData("script.exe")]
    [InlineData("graphic.svg")]
    public void Validator_WithInvalidExtensionsOrEmpty_ShouldHaveValidationError(string fileName)
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaImageCommand(stream, fileName, 100, "application/octet-stream", MediaFolder.Avatar);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validator_WithZeroOrNegativeLength_ShouldHaveValidationError()
    {
        using var stream = new MemoryStream();
        var command = new UploadMediaImageCommand(stream, "test.png", 0, "image/png", MediaFolder.Avatar);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileLength);
    }

    [Fact]
    public void Validator_WithLengthExceeding5MB_ShouldHaveValidationError()
    {
        using var stream = new MemoryStream();
        var command = new UploadMediaImageCommand(stream, "test.png", 5 * 1024 * 1024 + 1, "image/png", MediaFolder.Avatar);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileLength);
    }

    [Fact]
    public void Validator_WithInvalidFolder_ShouldHaveValidationError()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaImageCommand(stream, "test.png", 100, "image/png", (MediaFolder)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Folder);
    }
}
