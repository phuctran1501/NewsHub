using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.ValueObjects;
using Xunit;

namespace NewsHub.Domain.Tests.Entities;

public class MediaFileTests
{
    [Fact]
    public void CreateMediaFile_WithRequiredFields_ShouldHaveValidDefaults()
    {
        // Arrange
        var userId = Guid.CreateVersion7();

        // Act
        var mediaFile = new MediaFile
        {
            PublicId = "newshub/avatars/sample_abc123",
            Url = "https://res.cloudinary.com/newshub/image/upload/v1/newshub/avatars/sample_abc123.webp",
            OriginalFileName = "avatar.png",
            Format = "webp",
            Bytes = 102400,
            Width = 400,
            Height = 400,
            Folder = MediaFolder.Avatar,
            UploadedByUserId = userId
        };

        // Assert
        Assert.NotEqual(Guid.Empty, mediaFile.Id);
        Assert.Equal(7, mediaFile.Id.Version);
        Assert.Equal("newshub/avatars/sample_abc123", mediaFile.PublicId);
        Assert.Equal("https://res.cloudinary.com/newshub/image/upload/v1/newshub/avatars/sample_abc123.webp", mediaFile.Url);
        Assert.Equal("avatar.png", mediaFile.OriginalFileName);
        Assert.Equal("webp", mediaFile.Format);
        Assert.Equal(102400, mediaFile.Bytes);
        Assert.Equal(400, mediaFile.Width);
        Assert.Equal(400, mediaFile.Height);
        Assert.Equal(MediaFolder.Avatar, mediaFile.Folder);
        Assert.Equal(userId, mediaFile.UploadedByUserId);
        Assert.True(mediaFile.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(mediaFile.UploadedByUser);
    }

    [Fact]
    public void MediaFile_CanBeAssociated_WithUser()
    {
        // Arrange
        var user = new User
        {
            Email = new Email("journalist@newshub.com"),
            PasswordHash = "hashed_password",
            FullName = "Nhà Báo A",
            Role = UserRole.Journalist
        };

        var mediaFile = new MediaFile
        {
            PublicId = "newshub/article-covers/cover_01",
            Url = "https://res.cloudinary.com/newshub/image/upload/cover_01.webp",
            OriginalFileName = "cover.jpg",
            Format = "webp",
            Bytes = 204800,
            Width = 1200,
            Height = 630,
            Folder = MediaFolder.ArticleCover,
            UploadedByUserId = user.Id,
            UploadedByUser = user
        };

        // Act
        user.MediaFiles.Add(mediaFile);

        // Assert
        Assert.Single(user.MediaFiles);
        Assert.Contains(mediaFile, user.MediaFiles);
        Assert.Equal(user.Id, mediaFile.UploadedByUserId);
        Assert.Same(user, mediaFile.UploadedByUser);
    }

    [Theory]
    [InlineData(MediaFolder.Avatar, 1)]
    [InlineData(MediaFolder.ArticleCover, 2)]
    [InlineData(MediaFolder.ArticleBody, 3)]
    public void MediaFolder_Enum_ShouldHaveCorrectUnderlyingValues(MediaFolder folder, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)folder);
    }
}
