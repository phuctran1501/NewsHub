using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using NewsHub.API.Services;
using NewsHub.Domain.Enums;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class HttpCurrentUserTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public HttpCurrentUserTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    private HttpCurrentUser CreateCurrentUserWithClaims(IEnumerable<Claim>? claims, bool isAuthenticated = true)
    {
        var httpContext = new DefaultHttpContext();

        if (claims != null && isAuthenticated)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            httpContext.User = new ClaimsPrincipal(identity);
        }
        else
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        return new HttpCurrentUser(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void Properties_WhenAuthenticatedWithStandardClaims_ShouldExtractCorrectValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "journalist@newshub.vn";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, "Journalist")
        };

        var currentUser = CreateCurrentUserWithClaims(claims);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.Id);
        Assert.Equal(email, currentUser.Email);
        Assert.Equal(UserRole.Journalist, currentUser.Role);
        Assert.True(currentUser.IsInRole(UserRole.Journalist));
        Assert.False(currentUser.IsInRole(UserRole.Admin));
        Assert.False(currentUser.IsInRole(UserRole.User));
    }

    [Fact]
    public void Id_WhenTokenUsesSubClaim_ShouldFallbackAndExtractCorrectGuid()
    {
        // Arrange: Token phát hành từ OIDC/Identity Provider dùng claim "sub"
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("email", "admin@newshub.vn"),
            new Claim("role", "Admin")
        };

        var currentUser = CreateCurrentUserWithClaims(claims);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(userId, currentUser.Id);
        Assert.Equal("admin@newshub.vn", currentUser.Email);
        Assert.Equal(UserRole.Admin, currentUser.Role);
        Assert.True(currentUser.IsInRole(UserRole.Admin));
    }

    [Fact]
    public void Id_WhenTokenUsesIdClaim_ShouldFallbackAndExtractCorrectGuid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("id", userId.ToString()),
            new Claim("role", "User")
        };

        var currentUser = CreateCurrentUserWithClaims(claims);

        // Assert
        Assert.Equal(userId, currentUser.Id);
        Assert.Equal(UserRole.User, currentUser.Role);
    }

    [Fact]
    public void Properties_WhenUnauthenticatedGuest_ShouldReturnNullAndFalse()
    {
        // Arrange: Người dùng vãng lai chưa đăng nhập
        var currentUser = CreateCurrentUserWithClaims(null, isAuthenticated: false);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.Id);
        Assert.Null(currentUser.Email);
        Assert.Null(currentUser.Role);
        Assert.False(currentUser.IsInRole(UserRole.User));
        Assert.False(currentUser.IsInRole(UserRole.Admin));
    }

    [Fact]
    public void Properties_WhenHttpContextIsNull_ShouldSafelyReturnNullAndFalse()
    {
        // Arrange: Gọi ngoài scope web request (background job, startup)
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var currentUser = new HttpCurrentUser(_mockHttpContextAccessor.Object);

        // Assert
        Assert.False(currentUser.IsAuthenticated);
        Assert.Null(currentUser.Id);
        Assert.Null(currentUser.Email);
        Assert.Null(currentUser.Role);
        Assert.False(currentUser.IsInRole(UserRole.User));
    }

    [Fact]
    public void Id_WhenClaimIsNotValidGuid_ShouldReturnNullWithoutThrowing()
    {
        // Arrange: Claim chứa chuỗi không phải Guid hợp lệ
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-not-a-guid")
        };

        var currentUser = CreateCurrentUserWithClaims(claims);

        // Assert
        Assert.True(currentUser.IsAuthenticated);
        Assert.Null(currentUser.Id);
    }

    [Theory]
    [InlineData("0", UserRole.User)]
    [InlineData("1", UserRole.Journalist)]
    [InlineData("2", UserRole.Admin)]
    public void Role_WhenClaimIsNumericString_ShouldParseEnumCorrectly(string roleNumericString, UserRole expectedRole)
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, roleNumericString)
        };

        var currentUser = CreateCurrentUserWithClaims(claims);

        // Assert
        Assert.Equal(expectedRole, currentUser.Role);
        Assert.True(currentUser.IsInRole(expectedRole));
    }
}
