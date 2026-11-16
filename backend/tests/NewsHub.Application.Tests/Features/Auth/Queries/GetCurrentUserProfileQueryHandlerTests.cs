namespace NewsHub.Application.Tests.Features.Auth.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Queries.GetCurrentUserProfile;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetCurrentUserProfileQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    [Fact]
    public async Task Handle_WhenAuthenticated_ShouldReturnUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "me@example.com",
            PasswordHash = "hash",
            FullName = "Profile User",
            AvatarUrl = "https://example.com/avatar.jpg",
            Role = UserRole.Journalist,
            PhoneNumber = "+84912345678",
            PressCardNumber = "CARD-777",
            Bio = "Senior Editor",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-1),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var handler = new GetCurrentUserProfileQueryHandler(_dbContextMock.Object, _currentUserMock.Object);
        var query = new GetCurrentUserProfileQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("me@example.com", result.Email);
        Assert.Equal("Profile User", result.FullName);
        Assert.Equal("https://example.com/avatar.jpg", result.AvatarUrl);
        Assert.Equal(UserRole.Journalist, result.Role);
        Assert.Equal("+84912345678", result.PhoneNumber);
        Assert.Equal("CARD-777", result.PressCardNumber);
        Assert.Equal("Senior Editor", result.Bio);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = new GetCurrentUserProfileQueryHandler(_dbContextMock.Object, _currentUserMock.Object);
        var query = new GetCurrentUserProfileQuery();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserNotFoundInDatabase_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new GetCurrentUserProfileQueryHandler(_dbContextMock.Object, _currentUserMock.Object);
        var query = new GetCurrentUserProfileQuery();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.UserNotFound, ex.ErrorCode);
    }
}
