using System.Security.Claims;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Enums;

namespace NewsHub.API.Services;

/// <summary>
/// Triển khai ICurrentUser trích xuất thông tin người dùng từ JWT Claims trong HttpContext.
/// </summary>
public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? Id
    {
        get
        {
            if (!IsAuthenticated || User == null)
            {
                return null;
            }

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value
                ?? User.FindFirst("userId")?.Value;

            return Guid.TryParse(idClaim, out var userId) ? userId : null;
        }
    }

    public string? Email
    {
        get
        {
            if (!IsAuthenticated || User == null)
            {
                return null;
            }

            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value;
        }
    }

    public UserRole? Role
    {
        get
        {
            if (!IsAuthenticated || User == null)
            {
                return null;
            }

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;

            if (string.IsNullOrWhiteSpace(roleClaim))
            {
                return null;
            }

            if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role) && Enum.IsDefined(role))
            {
                return role;
            }

            if (int.TryParse(roleClaim, out var roleInt) && Enum.IsDefined(typeof(UserRole), roleInt))
            {
                return (UserRole)roleInt;
            }

            return null;
        }
    }

    public bool IsInRole(UserRole role)
    {
        if (!IsAuthenticated || User == null)
        {
            return false;
        }

        var currentRole = Role;
        if (currentRole.HasValue && currentRole.Value == role)
        {
            return true;
        }

        return User.IsInRole(role.ToString());
    }
}
