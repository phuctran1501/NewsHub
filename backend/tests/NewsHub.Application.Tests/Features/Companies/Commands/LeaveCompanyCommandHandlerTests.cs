namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.Commands.LeaveCompany;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class LeaveCompanyCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<LeaveCompanyCommandHandler>> _loggerMock = new();

    private LeaveCompanyCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenNotMember_ShouldThrowNotFoundException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new LeaveCompanyCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.NotCompanyMember, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenOwnerAttemptsToLeave_ShouldThrowBadRequestException()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var member = new CompanyMember
        {
            CompanyId = companyId,
            UserId = userId,
            Role = CompanyMemberRole.Owner
        };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { member }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new LeaveCompanyCommand(companyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotLeaveAsOwner, ex.ErrorCode);
    }
}
