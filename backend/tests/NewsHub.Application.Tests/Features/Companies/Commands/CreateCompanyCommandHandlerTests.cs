namespace NewsHub.Application.Tests.Features.Companies.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.Commands.CreateCompany;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class CreateCompanyCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CreateCompanyCommandHandler>> _loggerMock = new();

    private CreateCompanyCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new CreateCompanyCommand("Tòa Soạn Số", "0109988776"), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNotJournalist_ShouldThrowForbiddenException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(false);
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new CreateCompanyCommand("Tòa Soạn Số", "0109988776"), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCompanyNameExists_ShouldThrowConflictException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        var companies = new List<Company>
        {
            new() { Name = "Tòa Soạn Số", Code = "12345678", TaxCode = "9999999999" }
        };
        _dbContextMock.Setup(db => db.Companies).Returns(companies.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateCompanyCommand("tòa soạn số", "0109988776"), CancellationToken.None));
        Assert.Equal(ErrorCodes.CompanyAlreadyExists, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTaxCodeExists_ShouldThrowConflictException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid());

        var companies = new List<Company>
        {
            new() { Name = "Tòa Soạn Khác", Code = "12345678", TaxCode = "0109988776" }
        };
        _dbContextMock.Setup(db => db.Companies).Returns(companies.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateCompanyCommand("Tòa Soạn Mới", "0109988776"), CancellationToken.None));
        Assert.Equal(ErrorCodes.CompanyAlreadyExists, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldCreatePendingCompanyAndSave()
    {
        var journalistId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Journalist)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(journalistId);

        var companies = new List<Company>();
        var mockCompanies = companies.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Companies).Returns(mockCompanies.Object);

        var users = new List<User>
        {
            new() { Id = journalistId, FullName = "Nhà Báo A", Email = new Email("journalist@newshub.vn"), PasswordHash = "hash" }
        };
        _dbContextMock.Setup(db => db.Users).Returns(users.BuildMockDbSet().Object);

        var handler = CreateHandler();

        var result = await handler.Handle(new CreateCompanyCommand("Tòa Soạn Mới", "0109988776"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Tòa Soạn Mới", result.Name);
        Assert.Equal("0109988776", result.TaxCode);
        Assert.Equal(8, result.Code.Length);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
