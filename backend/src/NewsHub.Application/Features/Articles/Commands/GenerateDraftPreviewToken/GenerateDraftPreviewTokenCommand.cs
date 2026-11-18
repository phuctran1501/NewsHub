namespace NewsHub.Application.Features.Articles.Commands.GenerateDraftPreviewToken;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record GenerateDraftPreviewTokenCommand(Guid Id) : IRequest<DraftPreviewTokenDto>;

public sealed class GenerateDraftPreviewTokenCommandValidator : AbstractValidator<GenerateDraftPreviewTokenCommand>
{
    public GenerateDraftPreviewTokenCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class GenerateDraftPreviewTokenCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IDraftPreviewTokenService draftPreviewTokenService) : IRequestHandler<GenerateDraftPreviewTokenCommand, DraftPreviewTokenDto>
{
    public async Task<DraftPreviewTokenDto> Handle(
        GenerateDraftPreviewTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tạo liên kết xem thử bài nháp.", ErrorCodes.Unauthorized);
        }

        var article = await dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (article is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        var isAuthor = article.AuthorId == currentUser.Id;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isAuthor && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền tạo liên kết xem thử bài viết của người khác.", ErrorCodes.ArticleForbiddenEdit);
        }

        var lifetime = TimeSpan.FromHours(2);
        var token = draftPreviewTokenService.GeneratePreviewToken(article.Id, lifetime);
        var previewUrl = $"/articles/preview?token={token}";
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);

        return new DraftPreviewTokenDto(token, previewUrl, expiresAt);
    }
}
