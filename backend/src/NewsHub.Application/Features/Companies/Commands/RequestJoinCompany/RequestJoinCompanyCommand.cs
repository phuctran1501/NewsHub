namespace NewsHub.Application.Features.Companies.Commands.RequestJoinCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record RequestJoinCompanyCommand(Guid CompanyId) : IRequest<CompanyRequestDto>;

public sealed class RequestJoinCompanyCommandValidator : AbstractValidator<RequestJoinCompanyCommand>
{
    public RequestJoinCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");
    }
}

public sealed class RequestJoinCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<RequestJoinCompanyCommandHandler> logger) : IRequestHandler<RequestJoinCompanyCommand, CompanyRequestDto>
{
    public async Task<CompanyRequestDto> Handle(RequestJoinCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền xin gia nhập tòa soạn.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var company = await dbContext.Companies
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.Status == CompanyStatus.Approved, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn hoặc tòa soạn chưa được phê duyệt.", ErrorCodes.CompanyNotFound);
        }

        // Kiểm tra giới hạn 5 công ty
        var activeCount = await dbContext.CompanyMembers
            .CountAsync(m => m.UserId == userId, cancellationToken);
        if (activeCount >= 5)
        {
            throw new BadRequestException("Bạn đã tham gia tối đa 5 tòa soạn. Không thể xin gia nhập thêm.", ErrorCodes.MaxCompaniesLimitReached);
        }

        // Kiểm tra đã là thành viên chưa
        var isMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == userId, cancellationToken);
        if (isMember)
        {
            throw new ConflictException("Bạn đã là thành viên của tòa soạn này.", ErrorCodes.AlreadyCompanyMember);
        }

        // Kiểm tra đã có yêu cầu Pending chưa
        var hasPending = await dbContext.CompanyRequests
            .AnyAsync(r => r.CompanyId == request.CompanyId && r.JournalistId == userId && r.Type == CompanyRequestType.JoinRequest && r.Status == CompanyRequestStatus.Pending, cancellationToken);
        if (hasPending)
        {
            throw new ConflictException("Bạn đã gửi đơn xin gia nhập tòa soạn này và đang chờ duyệt.", ErrorCodes.JoinRequestAlreadyPending);
        }

        var companyRequest = new CompanyRequest
        {
            CompanyId = company.Id,
            JournalistId = userId,
            Type = CompanyRequestType.JoinRequest,
            Status = CompanyRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.CompanyRequests.Add(companyRequest);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Bạn đã có đơn xin gia nhập đang chờ phản hồi từ tòa soạn này.", ErrorCodes.JoinRequestAlreadyPending);
        }

        logger.LogInformation("Nhà báo {UserId} đã gửi đơn xin gia nhập công ty {CompanyId}.", userId, company.Id);

        var sender = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FullName, Email = ((string)u.Email), u.PressCardNumber })
            .FirstAsync(cancellationToken);

        // Bắn thông báo SignalR tới Owner của công ty
        try
        {
            var notification = new NotificationDto(
                Guid.CreateVersion7(),
                NotificationType.CompanyJoinRequest,
                "Đơn xin gia nhập mới",
                $"Nhà báo {sender.FullName} vừa gửi đơn xin gia nhập tòa soạn {company.Name}.",
                company.Id
            );
            await notificationService.SendToUserAsync(company.OwnerId, notification, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi gửi SignalR notification tới Owner {OwnerId}.", company.OwnerId);
        }

        return new CompanyRequestDto(
            companyRequest.Id,
            company.Id,
            company.Name,
            company.Code,
            userId,
            sender.FullName,
            sender.Email,
            sender.PressCardNumber,
            companyRequest.Type,
            companyRequest.Status,
            companyRequest.CreatedAt,
            companyRequest.RespondedAt
        );
    }
}
