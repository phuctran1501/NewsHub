namespace NewsHub.Application.Tests.Features.Companies.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.Queries.GetPublicCompanies;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Xunit;

public class GetPublicCompaniesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    public GetPublicCompaniesQueryHandlerTests()
    {
        _cacheServiceMock
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<CachedPublicCompaniesPage>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<CancellationToken, Task<CachedPublicCompaniesPage>> factory, TimeSpan? _, CancellationToken ct) =>
                factory(ct));
    }

    [Fact]
    public async Task Handle_ShouldReturnOnlyApprovedCompaniesSortedByPublishedArticlesAndHideTaxCode()
    {
        var approvedSmall = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Tòa soạn Nhỏ",
            Code = "11112222",
            TaxCode = "0101234567",
            Status = CompanyStatus.Approved,
            Owner = new User { Id = Guid.NewGuid(), FullName = "Nguyễn Văn A", Email = "a@test.vn", PasswordHash = "x" },
            Articles = new List<Article>
            {
                new() { Title = "Bài 1", Slug = "bai-1", ContentHtml = "<p>1</p>", Status = ArticleStatus.Published },
            },
            Members = new List<CompanyMember> { new() { UserId = Guid.NewGuid(), Role = CompanyMemberRole.Owner } },
            Followers = new List<CompanyFollower> { new() { UserId = Guid.NewGuid() } },
        };

        var approvedLarge = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Thời Báo Số",
            Code = "84920174",
            TaxCode = "0309998887",
            Status = CompanyStatus.Approved,
            Owner = new User { Id = Guid.NewGuid(), FullName = "Trần Thị B", Email = "b@test.vn", PasswordHash = "x" },
            Articles = new List<Article>
            {
                new() { Title = "Bài A", Slug = "bai-a", ContentHtml = "<p>a</p>", Status = ArticleStatus.Published },
                new() { Title = "Bài B", Slug = "bai-b", ContentHtml = "<p>b</p>", Status = ArticleStatus.Published },
                new() { Title = "Bài Nháp", Slug = "bai-nhap", ContentHtml = "<p>d</p>", Status = ArticleStatus.Draft },
            },
            Members = new List<CompanyMember>
            {
                new() { UserId = Guid.NewGuid(), Role = CompanyMemberRole.Owner },
                new() { UserId = Guid.NewGuid(), Role = CompanyMemberRole.Member },
            },
            Followers = new List<CompanyFollower>(),
        };

        var pendingCompany = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Tòa soạn Chờ Duyệt",
            Code = "99990000",
            TaxCode = "0909090909",
            Status = CompanyStatus.Pending,
        };

        _dbContextMock
            .Setup(db => db.Companies)
            .Returns(new List<Company> { approvedSmall, approvedLarge, pendingCompany }.BuildMockDbSet().Object);

        var handler = new GetPublicCompaniesQueryHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        var result = await handler.Handle(new GetPublicCompaniesQuery(null, 1, 6), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Thời Báo Số", result.Items[0].Name);
        Assert.Equal(2, result.Items[0].PublishedArticleCount);
        Assert.Equal(2, result.Items[0].MemberCount);
        Assert.Equal("Tòa soạn Nhỏ", result.Items[1].Name);
        Assert.Equal(1, result.Items[1].FollowerCount);
        Assert.Null(typeof(NewsHub.Application.Features.Companies.DTOs.PublicCompanyDto).GetProperty("TaxCode"));
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldFilterByNameOrCodeAndBypassDefaultCache()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Kinh Tế & Dữ Liệu",
            Code = "39104822",
            TaxCode = "0202020202",
            Status = CompanyStatus.Approved,
        };

        _dbContextMock
            .Setup(db => db.Companies)
            .Returns(new List<Company> { company }.BuildMockDbSet().Object);

        var handler = new GetPublicCompaniesQueryHandler(_dbContextMock.Object, _cacheServiceMock.Object);

        var result = await handler.Handle(new GetPublicCompaniesQuery("39104822", 1, 6), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Kinh Tế & Dữ Liệu", result.Items[0].Name);
        _cacheServiceMock.Verify(
            c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<CachedPublicCompaniesPage>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
