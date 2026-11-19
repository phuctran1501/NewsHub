namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.Commands.RespondToInvitation;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RespondToInvitationCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<RespondToInvitationCommandHandler>> _loggerMock = new();

    private RespondToInvitationCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RespondToInvitationCommand(Guid.NewGuid(), true), CancellationToken.None));
        Assert.Equal(ErrorCodes.RequestNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenInvitationNotPending_ShouldThrowBadRequestException()
    {
        var journalistId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var invite = new CompanyRequest
        {
            Id = requestId,
            JournalistId = journalistId,
            Type = CompanyRequestType.Invitation,
            Status = CompanyRequestStatus.Accepted
        };
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest> { invite }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new RespondToInvitationCommand(requestId, true), CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidRequestStatus, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAcceptingWith5Companies_ShouldThrowBadRequestException()
    {
        var journalistId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var invite = new CompanyRequest
        {
            Id = requestId,
            JournalistId = journalistId,
            Type = CompanyRequestType.Invitation,
            Status = CompanyRequestStatus.Pending
        };
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest> { invite }.BuildMockDbSet().Object);

        var members = Enumerable.Range(1, 5).Select(_ => new CompanyMember { UserId = journalistId }).ToList();
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new RespondToInvitationCommand(requestId, true), CancellationToken.None));
        Assert.Equal(ErrorCodes.MaxCompaniesLimitReached, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAccepting_ShouldAddMemberAndNotifyOwner()
    {
        var journalistId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var company = new Company { Id = companyId, Name = "Công Ty A", Code = "11112222", TaxCode = "010101", OwnerId = ownerId };
        var invite = new CompanyRequest
        {
            Id = requestId,
            CompanyId = companyId,
            JournalistId = journalistId,
            Type = CompanyRequestType.Invitation,
            Status = CompanyRequestStatus.Pending,
            Company = company
        };
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest> { invite }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        await handler.Handle(new RespondToInvitationCommand(requestId, true), CancellationToken.None);

        Assert.Equal(CompanyRequestStatus.Accepted, invite.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(ownerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRejecting_ShouldSetStatusRejected()
    {
        var journalistId = Guid.NewGuid();
        var requestId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var invite = new CompanyRequest
        {
            Id = requestId,
            JournalistId = journalistId,
            Type = CompanyRequestType.Invitation,
            Status = CompanyRequestStatus.Pending
        };
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest> { invite }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        await handler.Handle(new RespondToInvitationCommand(requestId, false), CancellationToken.None);

        Assert.Equal(CompanyRequestStatus.Rejected, invite.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
