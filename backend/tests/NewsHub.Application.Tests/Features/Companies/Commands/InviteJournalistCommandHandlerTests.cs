namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.Commands.InviteJournalist;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class InviteJournalistCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<InviteJournalistCommandHandler>> _loggerMock = new();

    private InviteJournalistCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenNotOwner_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new InviteJournalistCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.OnlyOwnerAllowed, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTargetUserNotFound_ShouldThrowNotFoundException()
    {
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var ownerMember = new CompanyMember { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { ownerMember }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { new() { Id = companyId, Name = "A", Code = "1", TaxCode = "2" } }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.Users).Returns(new List<User>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new InviteJournalistCommand(companyId, targetId), CancellationToken.None));
        Assert.Equal(ErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTargetNotJournalist_ShouldThrowBadRequestException()
    {
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var ownerMember = new CompanyMember { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { ownerMember }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { new() { Id = companyId, Name = "A", Code = "1", TaxCode = "2" } }.BuildMockDbSet().Object);

        var targetUser = new User
        {
            Id = targetId,
            Role = UserRole.User,
            FullName = "User",
            Email = new Email("u@test.com"),
            PasswordHash = "h"
        };
        _dbContextMock.Setup(db => db.Users).Returns(new List<User> { targetUser }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new InviteJournalistCommand(companyId, targetId), CancellationToken.None));
        Assert.Equal(ErrorCodes.UserIsNotJournalist, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateInvitationAndSendNotification()
    {
        var ownerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(ownerId);

        var ownerMember = new CompanyMember { CompanyId = companyId, UserId = ownerId, Role = CompanyMemberRole.Owner };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { ownerMember }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { new() { Id = companyId, Name = "Công Ty A", Code = "11112222", TaxCode = "010101" } }.BuildMockDbSet().Object);

        var targetUser = new User
        {
            Id = targetId,
            Role = UserRole.Journalist,
            FullName = "Nhà Báo B",
            Email = new Email("b@test.com"),
            PasswordHash = "h"
        };
        _dbContextMock.Setup(db => db.Users).Returns(new List<User> { targetUser }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new InviteJournalistCommand(companyId, targetId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CompanyRequestType.Invitation, result.Type);
        Assert.Equal(CompanyRequestStatus.Pending, result.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(targetId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
