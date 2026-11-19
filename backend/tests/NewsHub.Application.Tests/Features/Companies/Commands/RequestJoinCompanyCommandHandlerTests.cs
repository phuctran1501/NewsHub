namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.Commands.RequestJoinCompany;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class RequestJoinCompanyCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notifMock = new();
    private readonly Mock<ILogger<RequestJoinCompanyCommandHandler>> _loggerMock = new();

    private RequestJoinCompanyCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _notifMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenCompanyNotFoundOrNotApproved_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RequestJoinCompanyCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.CompanyNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenJournalistHas5ActiveCompanies_ShouldThrowBadRequestException()
    {
        var journalistId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var company = new Company { Id = companyId, Name = "Công Ty A", Code = "11112222", TaxCode = "010101", Status = CompanyStatus.Approved };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var members = Enumerable.Range(1, 5).Select(i => new CompanyMember
        {
            CompanyId = Guid.NewGuid(),
            UserId = journalistId
        }).ToList();
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new RequestJoinCompanyCommand(companyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.MaxCompaniesLimitReached, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAlreadyMember_ShouldThrowConflictException()
    {
        var journalistId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var company = new Company { Id = companyId, Name = "Công Ty A", Code = "11112222", TaxCode = "010101", Status = CompanyStatus.Approved };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var members = new List<CompanyMember>
        {
            new() { CompanyId = companyId, UserId = journalistId }
        };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(members.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RequestJoinCompanyCommand(companyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.AlreadyCompanyMember, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreateJoinRequestAndNotifyOwner()
    {
        var journalistId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var company = new Company { Id = companyId, Name = "Công Ty A", Code = "11112222", TaxCode = "010101", Status = CompanyStatus.Approved, OwnerId = ownerId };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyRequests).Returns(new List<CompanyRequest>().BuildMockDbSet().Object);

        var user = new User { Id = journalistId, FullName = "Nhà Báo X", Email = new Email("x@newshub.vn"), PasswordHash = "hash" };
        _dbContextMock.Setup(db => db.Users).Returns(new List<User> { user }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new RequestJoinCompanyCommand(companyId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(CompanyRequestStatus.Pending, result.Status);
        Assert.Equal(CompanyRequestType.JoinRequest, result.Type);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifMock.Verify(n => n.SendToUserAsync(ownerId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
