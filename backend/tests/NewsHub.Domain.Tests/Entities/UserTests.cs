using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.ValueObjects;
using Xunit;

namespace NewsHub.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void CreateUser_WithRequiredFields_ShouldHaveValidDefaults()
    {
        // Arrange & Act
        var user = new User
        {
            Email = new Email("reader@newshub.com"),
            PasswordHash = "argon2id$hashedpassword",
            FullName = "Nguyễn Văn Đọc Tin"
        };

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("reader@newshub.com", user.Email.Value);
        Assert.Equal("argon2id$hashedpassword", user.PasswordHash);
        Assert.Equal("Nguyễn Văn Đọc Tin", user.FullName);
        Assert.Equal(UserRole.User, user.Role);
        Assert.True(user.IsActive);
        Assert.True(user.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Null(user.UpdatedAt);
        Assert.Empty(user.RefreshTokens);
    }

    [Fact]
    public void UserRole_CanBeAssigned_ToJournalistOrAdmin()
    {
        // Arrange & Act
        var journalist = new User
        {
            Email = new Email("journalist@newshub.com"),
            PasswordHash = "hash",
            FullName = "Nhà Báo A",
            Role = UserRole.Journalist
        };

        var admin = new User
        {
            Email = new Email("admin@newshub.com"),
            PasswordHash = "hash",
            FullName = "Quản Trị Viên",
            Role = UserRole.Admin
        };

        // Assert
        Assert.Equal(UserRole.Journalist, journalist.Role);
        Assert.Equal(UserRole.Admin, admin.Role);
    }

    [Fact]
    public void CreateJournalistUser_WithPhoneNumberPressCardAndBio_ShouldStorePropertiesCorrectly()
    {
        // Arrange & Act
        var journalist = new User
        {
            Email = new Email("journalist@newshub.com"),
            PasswordHash = "hash",
            FullName = "Lê Văn Báo",
            Role = UserRole.Journalist,
            PhoneNumber = "+84912345678",
            PressCardNumber = "NB-2024-998811",
            Bio = "Phóng viên công nghệ & kinh tế số"
        };

        // Assert
        Assert.Equal("+84912345678", journalist.PhoneNumber);
        Assert.Equal("NB-2024-998811", journalist.PressCardNumber);
        Assert.Equal("Phóng viên công nghệ & kinh tế số", journalist.Bio);
        Assert.Equal(UserRole.Journalist, journalist.Role);
    }
}
