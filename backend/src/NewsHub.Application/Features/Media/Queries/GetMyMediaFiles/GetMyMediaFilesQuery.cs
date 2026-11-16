namespace NewsHub.Application.Features.Media.Queries.GetMyMediaFiles;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Media.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GetMyMediaFilesQuery(
    int PageNumber = 1,
    int PageSize = 12,
    MediaFolder? Folder = null) : IRequest<PagedList<MediaFileResponseDto>>;

public sealed class GetMyMediaFilesQueryValidator : AbstractValidator<GetMyMediaFilesQuery>
{
    public GetMyMediaFilesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .InclusiveBetween(1, 10_000)
            .WithMessage("Số trang phải từ 1 đến 10,000.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("Kích thước trang phải từ 1 đến 50.");

        When(x => x.Folder.HasValue, () =>
        {
            RuleFor(x => x.Folder)
                .IsInEnum()
                .WithMessage("Thư mục lưu trữ hình ảnh không hợp lệ.");
        });
    }
}

public sealed class GetMyMediaFilesQueryHandler(
    ICurrentUser currentUser,
    IApplicationDbContext dbContext) : IRequestHandler<GetMyMediaFilesQuery, PagedList<MediaFileResponseDto>>
{
    public async Task<PagedList<MediaFileResponseDto>> Handle(
        GetMyMediaFilesQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Người dùng chưa đăng nhập hoặc phiên làm việc đã hết hạn.", ErrorCodes.Unauthorized);
        }

        var query = dbContext.MediaFiles
            .AsNoTracking()
            .Where(m => m.UploadedByUserId == currentUser.Id.Value);

        if (request.Folder.HasValue)
        {
            query = query.Where(m => m.Folder == request.Folder.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.PageNumber - 1) * request.PageSize;
        if (offset > int.MaxValue)
        {
            return new PagedList<MediaFileResponseDto>([], request.PageNumber, request.PageSize, totalCount);
        }

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Select(m => new MediaFileResponseDto(
                m.Id,
                m.PublicId,
                m.Url,
                m.OriginalFileName,
                m.Format,
                m.Bytes,
                m.Width,
                m.Height,
                m.Folder,
                m.UploadedByUserId,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<MediaFileResponseDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
