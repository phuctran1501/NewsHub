namespace NewsHub.Application.Tests.Features.Admin.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.Commands.EmergencyAssignCompanyOwner;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class EmergencyAssignCompanyOwnerCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<EmergencyAssignCompanyOwnerCommandHandler>> _loggerMock = new();

    private EmergencyAssignCompanyOwnerCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenNotAdmin_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new EmergencyAssignCompanyOwnerCommand(Guid.NewGuid(), Guid.NewGuid(), "Reason"), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCompanyNotFound_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new EmergencyAssignCompanyOwnerCommand(Guid.NewGuid(), Guid.NewGuid(), "Reason"), CancellationToken.None));
        Assert.Equal(ErrorCodes.CompanyNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTargetNotMember_ShouldThrowBadRequestException()
    {
        var companyId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2" };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new EmergencyAssignCompanyOwnerCommand(companyId, targetId, "Reason"), CancellationToken.None));
        Assert.Equal(ErrorCodes.NotCompanyMember, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldReassignOwnerAndNotify()
    {
        var companyId = Guid.NewGuid();
        var oldOwnerId = Guid.NewGuid();
        var newOwnerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", OwnerId = oldOwnerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var oldOwnerMember = new CompanyMember { CompanyId = companyId, UserId = oldOwnerId, Role = CompanyMemberRole.Owner };
        var newOwnerMember = new CompanyMember { CompanyId = companyId, UserId = newOwnerId, Role = CompanyMemberRole.Member };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { oldOwnerMember, newOwnerMember }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        await handler.Handle(new EmergencyAssignCompanyOwnerCommand(companyId, newOwnerId, "Owner cũ bị khóa tài khoản."), CancellationToken.None);

        Assert.Equal(newOwnerId, company.OwnerId);
        Assert.Equal(CompanyMemberRole.Member, oldOwnerMember.Role);
        Assert.Equal(CompanyMemberRole.Owner, newOwnerMember.Role);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(newOwnerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
