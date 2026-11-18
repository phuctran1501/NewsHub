namespace NewsHub.Infrastructure.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NewsHub.Application.Common.Interfaces;

/// <summary>
/// Triển khai IDraftPreviewTokenService sử dụng JWT ký số HMAC-SHA256 với thời hạn mặc định 2 giờ.
/// </summary>
public class DraftPreviewTokenService : IDraftPreviewTokenService
{
    private const string PreviewPurpose = "draft_preview";
    private readonly JwtOptions _options;
    private readonly ILogger<DraftPreviewTokenService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public DraftPreviewTokenService(
        IOptions<JwtOptions> options,
        ILogger<DraftPreviewTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GeneratePreviewToken(Guid articleId, TimeSpan? lifetime = null)
    {
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(2));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, articleId.ToString()),
            new("article_id", articleId.ToString()),
            new("purpose", PreviewPurpose),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return _tokenHandler.WriteToken(token);
    }

    public bool ValidatePreviewToken(string token, out Guid articleId)
    {
        articleId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var key = Encoding.UTF8.GetBytes(_options.SecretKey);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != PreviewPurpose)
            {
                return false;
            }

            var idString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst("article_id")?.Value;

            return Guid.TryParse(idString, out articleId);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogInformation("Preview token đã hết hạn");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Preview token không hợp lệ hoặc bị giả mạo");
            return false;
        }
    }
}
