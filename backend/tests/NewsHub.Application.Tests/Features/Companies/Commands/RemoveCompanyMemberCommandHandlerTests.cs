namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.Commands.RemoveCompanyMember;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RemoveCompanyMemberCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<RemoveCompanyMemberCommandHandler>> _loggerMock = new();

    private RemoveCompanyMemberCommandHandler CreateHandler() =>
        new(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _unitOfWorkMock.Object,
            _notifMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenTargetIsOwner_ShouldThrowBadRequestException()
    {
        var ownerId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var callerMember = new CompanyMember
        {
            CompanyId = companyId,
            UserId = ownerId,
            Role = CompanyMemberRole.Owner
        };

        var targetMember = new CompanyMember
        {
            CompanyId = companyId,
            UserId = targetOwnerId,
            Role = CompanyMemberRole.Owner
        };

        _dbContextMock.Setup(db => db.CompanyMembers)
            .Returns(new List<CompanyMember> { callerMember, targetMember }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new RemoveCompanyMemberCommand(companyId, targetOwnerId), CancellationToken.None));

        Assert.Equal(ErrorCodes.CannotRemoveOwner, ex.ErrorCode);
    }
}
