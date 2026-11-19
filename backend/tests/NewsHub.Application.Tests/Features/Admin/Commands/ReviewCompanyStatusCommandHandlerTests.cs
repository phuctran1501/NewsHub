namespace NewsHub.Application.Tests.Features.Admin.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.Commands.ReviewCompanyStatus;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class ReviewCompanyStatusCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<ReviewCompanyStatusCommandHandler>> _loggerMock = new();

    private ReviewCompanyStatusCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenNotAdmin_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ReviewCompanyStatusCommand(Guid.NewGuid(), "Approved"), CancellationToken.None));
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
            handler.Handle(new ReviewCompanyStatusCommand(Guid.NewGuid(), "Approved"), CancellationToken.None));
        Assert.Equal(ErrorCodes.CompanyNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCompanyNotPending_ShouldThrowBadRequestException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = Guid.NewGuid(), Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Approved };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new ReviewCompanyStatusCommand(company.Id, "Approved"), CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidCompanyStatus, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAdminApproves_ShouldSetApprovedAndAddOwnerToMembers()
    {
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Pending, OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        await handler.Handle(new ReviewCompanyStatusCommand(companyId, "Approved"), CancellationToken.None);

        Assert.Equal(CompanyStatus.Approved, company.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(ownerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAdminRejects_ShouldSetRejectedAndNotifyOwner()
    {
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Pending, OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        await handler.Handle(new ReviewCompanyStatusCommand(companyId, "Rejected", "Mã số thuế không hợp lệ"), CancellationToken.None);

        Assert.Equal(CompanyStatus.Rejected, company.Status);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(ownerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAdminApprovesAndOwnerAlreadyIn5Companies_ShouldThrowBadRequestException()
    {
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Pending, OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var members = Enumerable.Range(1, 5).Select(_ => new CompanyMember { UserId = ownerId }).ToList();
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new ReviewCompanyStatusCommand(companyId, "Approved"), CancellationToken.None));
        Assert.Equal(ErrorCodes.MaxCompaniesLimitReached, ex.ErrorCode);
    }
}
