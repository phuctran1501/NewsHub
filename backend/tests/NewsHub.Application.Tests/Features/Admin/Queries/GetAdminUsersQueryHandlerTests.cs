namespace NewsHub.Application.Tests.Features.Admin.Queries;

using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Queries.GetAdminUsers;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class GetAdminUsersQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetAdminUsersQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetAdminUsersQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetAdminUsersQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValidQueryWithoutFilter_ShouldReturnPagedUsers()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var usersList = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("user1@example.com"),
                FullName = "Nguyễn Văn Một",
                PasswordHash = "hash1",
                Role = UserRole.User,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("user2@example.com"),
                FullName = "Trần Văn Hai",
                PasswordHash = "hash2",
                Role = UserRole.Journalist,
                IsActive = false
            }
        };

        _dbContextMock.Setup(db => db.Users).Returns(usersList.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAdminUsersQuery(1, 10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_WhenFilteringByRoleAndActiveStatus_ShouldReturnMatchingUsers()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var usersList = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("journalist@example.com"),
                FullName = "Nhà Báo A",
                PasswordHash = "hash1",
                Role = UserRole.Journalist,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("lockedjournalist@example.com"),
                FullName = "Nhà Báo B",
                PasswordHash = "hash2",
                Role = UserRole.Journalist,
                IsActive = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("user@example.com"),
                FullName = "Độc Giả C",
                PasswordHash = "hash3",
                Role = UserRole.User,
                IsActive = true
            }
        };

        _dbContextMock.Setup(db => db.Users).Returns(usersList.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act: Filter by Role = Journalist and IsActive = true
        var result = await handler.Handle(new GetAdminUsersQuery(1, 10, Role: UserRole.Journalist, IsActive: true), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Nhà Báo A", result.Items[0].FullName);
    }

    [Fact]
    public async Task Handle_WhenSearchingByNameOrEmail_ShouldReturnMatchingUsers()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var usersList = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("special.agent@example.com"),
                FullName = "Hoàng Nam",
                PasswordHash = "hash1",
                Role = UserRole.User,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = new Email("other@example.com"),
                FullName = "Lê Hoàng",
                PasswordHash = "hash2",
                Role = UserRole.User,
                IsActive = true
            }
        };

        _dbContextMock.Setup(db => db.Users).Returns(usersList.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act: Search for "special"
        var result = await handler.Handle(new GetAdminUsersQuery(1, 10, SearchTerm: "special"), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("special.agent@example.com", result.Items[0].Email);
    }
}
