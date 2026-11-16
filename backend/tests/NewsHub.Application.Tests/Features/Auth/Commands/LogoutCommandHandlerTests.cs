namespace NewsHub.Application.Tests.Features.Auth.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.Logout;
using NewsHub.Domain.Entities;
using Xunit;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();

    public LogoutCommandHandlerTests()
    {
        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("valid_raw_token")).Returns("hashed_token");
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRevokeTokenAndSave()
    {
        // Arrange
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = "hashed_token",
            IsRevoked = false,
            RevokedAt = null
        };

        var tokens = new List<RefreshToken> { token }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new LogoutCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new LogoutCommand("valid_raw_token");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenTokenIsNullOrEmpty_ShouldDoNothing(string? token)
    {
        var handler = new LogoutCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new LogoutCommand(token);

        await handler.Handle(command, CancellationToken.None);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldDoNothing()
    {
        var tokens = new List<RefreshToken>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);

        var handler = new LogoutCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new LogoutCommand("valid_raw_token");

        await handler.Handle(command, CancellationToken.None);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
