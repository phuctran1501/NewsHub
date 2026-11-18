namespace NewsHub.Application.Tests.Features.ReadingHistory.Commands;

using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.ReadingHistory.Commands.ClearAllReadingHistory;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;
using Xunit;

public class ClearAllReadingHistoryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private ClearAllReadingHistoryCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ClearAllReadingHistoryCommand(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }
}
