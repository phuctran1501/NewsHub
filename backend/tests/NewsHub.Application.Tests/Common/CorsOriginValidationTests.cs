namespace NewsHub.Application.Tests.Common;

using NewsHub.API.Extensions;
using Xunit;

public class CorsOriginValidationTests
{
    private readonly string[] _configuredOrigins =
    [
        "https://newshub.vercel.app",
        "https://staging.newshub.vn"
    ];

    [Theory]
    [InlineData("https://newshub.vercel.app")]
    [InlineData("https://staging.newshub.vn")]
    public void IsOriginAllowed_WithConfiguredOrigins_ReturnsTrue(string origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.True(result);
    }

    [Theory]
    [InlineData("https://newshub-git-feat-test-phuctran.vercel.app")]
    [InlineData("https://newshub-preview-123.vercel.app")]
    [InlineData("https://vercel.app")]
    public void IsOriginAllowed_WithVercelSubdomainsOverHttps_ReturnsTrue(string origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.True(result);
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:4173")]
    [InlineData("https://localhost:7001")]
    [InlineData("http://127.0.0.1:5173")]
    public void IsOriginAllowed_WithLocalhostAnd127001AnyPort_ReturnsTrue(string origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.True(result);
    }

    [Theory]
    [InlineData("http://newshub.vercel.app")] // Không an toàn (plain HTTP trên cloud)
    [InlineData("http://newshub-git-feat.vercel.app")]
    public void IsOriginAllowed_WithVercelOverInsecureHttp_ReturnsFalse(string origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.False(result);
    }

    [Theory]
    [InlineData("https://evilvercel.app")] // Domain giả mạo (thiếu dấu chấm trước vercel.app)
    [InlineData("https://notvercel.app")]
    [InlineData("https://attacker-site.com")]
    [InlineData("https://newshub.com")]
    public void IsOriginAllowed_WithUnauthorizedExternalDomains_ReturnsFalse(string origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-uri")]
    public void IsOriginAllowed_WithInvalidOrEmptyOrigin_ReturnsFalse(string? origin)
    {
        var result = CorsExtensions.IsOriginAllowed(origin, _configuredOrigins);

        Assert.False(result);
    }
}
