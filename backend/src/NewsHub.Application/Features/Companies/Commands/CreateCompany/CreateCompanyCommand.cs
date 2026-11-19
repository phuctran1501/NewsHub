namespace NewsHub.Application.Features.Companies.Commands.CreateCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record CreateCompanyCommand(string Name, string TaxCode) : IRequest<CompanySummaryDto>;

public sealed class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên công ty / tòa soạn không được để trống.")
            .MinimumLength(3).WithMessage("Tên công ty phải có ít nhất 3 ký tự.")
            .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự.");

        RuleFor(x => x.TaxCode)
            .NotEmpty().WithMessage("Mã số thuế không được để trống.")
            .Matches(@"^[0-9]{1,10}$").WithMessage("Mã số thuế phải là chuỗi số từ 1 đến 10 ký tự.");
    }
}

public sealed class CreateCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ILogger<CreateCompanyCommandHandler> logger) : IRequestHandler<CreateCompanyCommand, CompanySummaryDto>
{
    public async Task<CompanySummaryDto> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện chức năng này.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo (Journalist) mới có quyền đăng ký thành lập tòa soạn / công ty.", ErrorCodes.PermissionDenied);
        }

        var normalizedName = request.Name.Trim();
        var normalizedTaxCode = request.TaxCode.Trim();

        var nameExists = await dbContext.Companies
            .AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
        if (nameExists)
        {
            throw new ConflictException("Tên công ty / tòa soạn đã tồn tại trên hệ thống.", ErrorCodes.CompanyAlreadyExists);
        }

        var taxCodeExists = await dbContext.Companies
            .AnyAsync(c => c.TaxCode == normalizedTaxCode, cancellationToken);
        if (taxCodeExists)
        {
            throw new ConflictException("Mã số thuế này đã được đăng ký bởi công ty khác.", ErrorCodes.CompanyAlreadyExists);
        }

        // Sinh mã 8 chữ số ngẫu nhiên duy nhất toàn cục
        string code;
        var maxAttempts = 10;
        var attempts = 0;
        do
        {
            code = Random.Shared.Next(10000000, 99999999).ToString();
            attempts++;
            var codeExists = await dbContext.Companies.AnyAsync(c => c.Code == code, cancellationToken);
            if (!codeExists)
            {
                break;
            }
        } while (attempts < maxAttempts);

        var ownerId = currentUser.Id!.Value;

        var activeCount = await dbContext.CompanyMembers
            .CountAsync(m => m.UserId == ownerId, cancellationToken);
        if (activeCount >= 5)
        {
            throw new BadRequestException("Bạn đã tham gia tối đa 5 tòa soạn. Không thể đăng ký thành lập thêm.", ErrorCodes.MaxCompaniesLimitReached);
        }

        var company = new Company
        {
            Name = normalizedName,
            TaxCode = normalizedTaxCode,
            Code = code,
            Status = CompanyStatus.Pending,
            OwnerId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Companies.Add(company);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Tên, mã số thuế hoặc mã định danh của công ty bị trùng lặp.", ErrorCodes.CompanyAlreadyExists);
        }

        logger.LogInformation("Nhà báo {OwnerId} đã gửi đơn thành lập công ty {CompanyName} với mã {Code}, đang chờ Admin duyệt.",
            ownerId, company.Name, company.Code);

        var owner = await dbContext.Users
            .Where(u => u.Id == ownerId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return new CompanySummaryDto(
            company.Id,
            company.Name,
            company.Code,
            company.TaxCode,
            owner,
            0,
            0,
            company.CreatedAt
        );
    }
}
