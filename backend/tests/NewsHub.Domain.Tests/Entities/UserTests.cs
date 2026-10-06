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
}
