namespace NewsHub.Application.Tests.Features.Media.Queries;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Media.Queries.GetMyMediaFiles;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetMyMediaFilesQueryHandlerTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly GetMyMediaFilesQueryValidator _validator = new();

    [Fact]
    public async Task Handle_WithoutFolderFilter_ShouldReturnOnlyCurrentUserMediaPaged()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(currentUserId);

        var mediaFiles = new List<MediaFile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PublicId = "pub1",
                Url = "url1",
                OriginalFileName = "1.webp",
                Format = "webp",
                Folder = MediaFolder.Avatar,
                UploadedByUserId = currentUserId,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
            },
            new()
            {
                Id = Guid.NewGuid(),
                PublicId = "pub2",
                Url = "url2",
                OriginalFileName = "2.webp",
                Format = "webp",
                Folder = MediaFolder.ArticleCover,
                UploadedByUserId = currentUserId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                PublicId = "pub3",
                Url = "url3",
                OriginalFileName = "3.webp",
                Format = "webp",
                Folder = MediaFolder.Avatar,
                UploadedByUserId = otherUserId,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var mockDbSet = mediaFiles.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mockDbSet.Object);

        var handler = new GetMyMediaFilesQueryHandler(_currentUserMock.Object, _dbContextMock.Object);

        // Act
        var result = await handler.Handle(new GetMyMediaFilesQuery(1, 10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("pub2", result.Items[0].PublicId); // Sorted by CreatedAt DESC
        Assert.Equal("pub1", result.Items[1].PublicId);
    }

    [Fact]
    public async Task Handle_WithFolderFilter_ShouldReturnOnlyCurrentUserMediaInSpecifiedFolder()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(currentUserId);

        var mediaFiles = new List<MediaFile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PublicId = "pub_avatar",
                Url = "url_avatar",
                OriginalFileName = "avatar.webp",
                Format = "webp",
                Folder = MediaFolder.Avatar,
                UploadedByUserId = currentUserId,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                PublicId = "pub_cover",
                Url = "url_cover",
                OriginalFileName = "cover.webp",
                Format = "webp",
                Folder = MediaFolder.ArticleCover,
                UploadedByUserId = currentUserId,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var mockDbSet = mediaFiles.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MediaFiles).Returns(mockDbSet.Object);

        var handler = new GetMyMediaFilesQueryHandler(_currentUserMock.Object, _dbContextMock.Object);

        // Act
        var result = await handler.Handle(new GetMyMediaFilesQuery(1, 10, MediaFolder.ArticleCover), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("pub_cover", result.Items[0].PublicId);
        Assert.Equal(MediaFolder.ArticleCover, result.Items[0].Folder);
    }

    [Fact]
    public async Task Handle_WhenUserUnauthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);

        var handler = new GetMyMediaFilesQueryHandler(_currentUserMock.Object, _dbContextMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyMediaFilesQuery(1, 12), CancellationToken.None));

        Assert.Equal(ErrorCodes.Unauthorized, exception.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_WithInvalidPageNumber_ShouldHaveValidationError(int pageNumber)
    {
        var query = new GetMyMediaFilesQuery(pageNumber, 12);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)]
    [InlineData(100)]
    public void Validator_WithInvalidPageSize_ShouldHaveValidationError(int pageSize)
    {
        var query = new GetMyMediaFilesQuery(1, pageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_WithInvalidFolderEnum_ShouldHaveValidationError()
    {
        var query = new GetMyMediaFilesQuery(1, 12, (MediaFolder)999);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Folder);
    }

    [Fact]
    public void Validator_WithValidParameters_ShouldNotHaveValidationError()
    {
        var query = new GetMyMediaFilesQuery(1, 12, MediaFolder.Avatar);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
