namespace NewsHub.Application.Tests.Features.Media.Commands;

using FluentValidation.TestHelper;
using MediatR;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Media.Commands.DeleteMediaImage;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class DeleteMediaImageCommandHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly DeleteMediaImageCommandValidator _validator = new();

    public DeleteMediaImageCommandHandlerTests()
    {
        _dbContextMock.Setup(db => db.Articles).Returns(new List<Article>().BuildMockDbSet().Object);
    }

    [Fact]
    public async Task Handle_WhenUserIsOwner_ShouldDeleteFromCloudinaryAndDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/pub_1",
            Url = "https://res.cloudinary.com/demo/test/pub_1.webp",
            OriginalFileName = "test.webp",
            Format = "webp",
            UploadedByUserId = userId
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(mediaFile.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("test/pub_1", It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.MediaFiles.Remove(mediaFile), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsAdminAndNotOwner_ShouldDeleteSuccessfully()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(adminId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(true);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/admin_delete",
            Url = "https://res.cloudinary.com/demo/test/admin_delete.webp",
            OriginalFileName = "admin_delete.webp",
            Format = "webp",
            UploadedByUserId = ownerId
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(mediaFile.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync("test/admin_delete", It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.MediaFiles.Remove(mediaFile), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMediaNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var mediaList = new List<MediaFile>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteMediaImageCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(ErrorCodes.MediaNotFound, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwnerAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(currentUserId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/not_owner",
            Url = "https://res.cloudinary.com/demo/test/not_owner.webp",
            OriginalFileName = "not_owner.webp",
            Format = "webp",
            UploadedByUserId = otherUserId
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None));

        Assert.Equal(ErrorCodes.PermissionDenied, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenMediaIsInUseAsAvatar_ShouldThrowConflictException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var imageUrl = "https://res.cloudinary.com/demo/test/in_use.webp";

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/in_use",
            Url = imageUrl,
            OriginalFileName = "in_use.webp",
            Format = "webp",
            UploadedByUserId = userId
        };

        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "hash",
            FullName = "Test User",
            AvatarUrl = imageUrl
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User> { user }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None));

        Assert.Equal(ErrorCodes.MediaInUse, exception.ErrorCode);
        _cloudinaryMock.Verify(c => c.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _dbContextMock.Verify(db => db.MediaFiles.Remove(It.IsAny<MediaFile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCloudinaryDeleteFails_ShouldThrowBadRequestExceptionAndNotDeleteDatabaseRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/fail_delete",
            Url = "https://res.cloudinary.com/demo/test/fail_delete.webp",
            OriginalFileName = "fail.webp",
            Format = "webp",
            UploadedByUserId = userId
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);

        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(mediaFile.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None));

        Assert.Equal(ErrorCodes.MediaDeleteFailed, exception.ErrorCode);
        _dbContextMock.Verify(db => db.MediaFiles.Remove(It.IsAny<MediaFile>()), Times.Never);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAvatarAssignedDuringDeletion_ShouldNullifyUserAvatarUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var imageUrl = "https://res.cloudinary.com/demo/test/race.webp";

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/race",
            Url = imageUrl,
            OriginalFileName = "race.webp",
            Format = "webp",
            UploadedByUserId = userId
        };

        var user = new User
        {
            Id = userId,
            Email = "race@example.com",
            PasswordHash = "hash",
            FullName = "Race User",
            AvatarUrl = null // Initially not avatar, so passes first check
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User> { user }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Simulate user assigning avatar during Cloudinary deletion
        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(mediaFile.PublicId, It.IsAny<CancellationToken>()))
            .Callback(() => user.AvatarUrl = imageUrl)
            .ReturnsAsync(true);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        Assert.Null(user.AvatarUrl); // AvatarUrl was nullified by secondary check
        _dbContextMock.Verify(db => db.MediaFiles.Remove(mediaFile), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new DeleteMediaImageCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(ErrorCodes.Unauthorized, exception.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCoverImageUsedByArticles_ShouldInvalidateCachedArticles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var articleSlug = "bai-viet-mau";

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var mediaFile = new MediaFile
        {
            Id = mediaId,
            PublicId = "test/cover_image",
            Url = "https://res.cloudinary.com/demo/test/cover.webp",
            OriginalFileName = "cover.webp",
            Format = "webp",
            UploadedByUserId = userId
        };

        var article = new Article
        {
            Id = articleId,
            Title = "Bài viết mẫu",
            Slug = articleSlug,
            CoverImageId = mediaId,
            AuthorId = userId,
            ContentHtml = "<p>Test</p>",
            Status = ArticleStatus.Published
        };

        var mediaList = new List<MediaFile> { mediaFile }.BuildMockDbSet();
        var usersList = new List<User>().BuildMockDbSet();
        var articlesList = new List<Article> { article }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.MediaFiles).Returns(mediaList.Object);
        _dbContextMock.Setup(db => db.Users).Returns(usersList.Object);
        _dbContextMock.Setup(db => db.Articles).Returns(articlesList.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _cloudinaryMock
            .Setup(c => c.DeleteImageAsync(mediaFile.PublicId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteMediaImageCommandHandler(
            _currentUserMock.Object,
            _cloudinaryMock.Object,
            _dbContextMock.Object,
            _cacheServiceMock.Object);

        // Act
        var result = await handler.Handle(new DeleteMediaImageCommand(mediaId), CancellationToken.None);

        // Assert
        Assert.Equal(Unit.Value, result);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"articles:detail:{articleId}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"articles:slug:{articleSlug}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Validator_WithEmptyId_ShouldHaveValidationError()
    {
        var command = new DeleteMediaImageCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_WithValidId_ShouldNotHaveValidationError()
    {
        var command = new DeleteMediaImageCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
