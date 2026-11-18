namespace NewsHub.Application.Features.Admin.Queries.GetAdminUsers;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

/// <summary>
/// Query lấy danh sách người dùng toàn hệ thống có phân trang, hỗ trợ bộ lọc vai trò, trạng thái kích hoạt và tìm kiếm.
/// </summary>
public sealed record GetAdminUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    UserRole? Role = null,
    bool? IsActive = null,
    string? SearchTerm = null) : IRequest<PagedList<AdminUserSummaryDto>>;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetAdminUsersQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetAdminUsersQuery, PagedList<AdminUserSummaryDto>>
{
    public async Task<PagedList<AdminUserSummaryDto>> Handle(
        GetAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền xem danh sách người dùng.", ErrorCodes.PermissionDenied);
        }

        var query = dbContext.Users.AsNoTracking();

        if (request.Role.HasValue)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(search) || u.Email.Value.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new AdminUserSummaryDto(
                u.Id,
                u.Email.Value,
                u.FullName,
                u.AvatarUrl,
                u.PhoneNumber,
                u.PressCardNumber,
                u.Bio,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<AdminUserSummaryDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
