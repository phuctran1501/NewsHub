namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.Commands.FollowCompany;
using NewsHub.Application.Features.Companies.Commands.UnfollowCompany;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class FollowCompanyCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<FollowCompanyCommandHandler>> _followLoggerMock = new();
    private readonly Mock<ILogger<UnfollowCompanyCommandHandler>> _unfollowLoggerMock = new();

    [Fact]
    public async Task Follow_WhenCompanyMemberAttemptsToFollowOwnCompany_ShouldThrowBadRequestException()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Approved };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var member = new CompanyMember { CompanyId = companyId, UserId = userId, Role = CompanyMemberRole.Member };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { member }.BuildMockDbSet().Object);

        var handler = new FollowCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _followLoggerMock.Object);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new FollowCompanyCommand(companyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotFollowOwnCompany, ex.ErrorCode);
    }

    [Fact]
    public async Task Follow_WhenValidReader_ShouldAddFollowerAndSave()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var company = new Company { Id = companyId, Name = "C", Code = "1", TaxCode = "2", Status = CompanyStatus.Approved };
        _dbContextMock.Setup(db => db.Companies).Returns(new List<Company> { company }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.CompanyFollowers).Returns(new List<CompanyFollower>().BuildMockDbSet().Object);

        var handler = new FollowCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _followLoggerMock.Object);

        await handler.Handle(new FollowCompanyCommand(companyId), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unfollow_WhenMemberAttemptsToUnfollow_ShouldThrowBadRequestException()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var member = new CompanyMember { CompanyId = companyId, UserId = userId, Role = CompanyMemberRole.Member };
        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember> { member }.BuildMockDbSet().Object);

        var handler = new UnfollowCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _unfollowLoggerMock.Object);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new UnfollowCompanyCommand(companyId), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotUnfollowOwnCompany, ex.ErrorCode);
    }

    [Fact]
    public async Task Unfollow_WhenValidFollower_ShouldRemoveFollowerAndSave()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        _dbContextMock.Setup(db => db.CompanyMembers).Returns(new List<CompanyMember>().BuildMockDbSet().Object);
        var follower = new CompanyFollower { CompanyId = companyId, UserId = userId };
        _dbContextMock.Setup(db => db.CompanyFollowers).Returns(new List<CompanyFollower> { follower }.BuildMockDbSet().Object);

        var handler = new UnfollowCompanyCommandHandler(
            _dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _unfollowLoggerMock.Object);

        await handler.Handle(new UnfollowCompanyCommand(companyId), CancellationToken.None);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
