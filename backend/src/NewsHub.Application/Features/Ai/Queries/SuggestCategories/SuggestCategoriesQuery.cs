namespace NewsHub.Application.Features.Ai.Queries.SuggestCategories;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record SuggestCategoriesQuery(
    string Title,
    string ContentSnippet) : IRequest<SuggestCategoriesResponseDto>;

public sealed class SuggestCategoriesQueryValidator : AbstractValidator<SuggestCategoriesQuery>
{
    public SuggestCategoriesQueryValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề bài viết không được để trống.")
            .MaximumLength(300).WithMessage("Tiêu đề bài viết không được vượt quá 300 ký tự.");

        RuleFor(x => x.ContentSnippet)
            .NotEmpty().WithMessage("Đoạn trích nội dung không được để trống.")
            .MaximumLength(5000).WithMessage("Đoạn trích nội dung không được vượt quá 5000 ký tự.");
    }
}

public sealed class SuggestCategoriesQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IAiService aiService) : IRequestHandler<SuggestCategoriesQuery, SuggestCategoriesResponseDto>
{
    public async Task<SuggestCategoriesResponseDto> Handle(
        SuggestCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để sử dụng tính năng gợi ý chuyên mục AI.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist) && !currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ nhà báo hoặc quản trị viên mới có quyền sử dụng tính năng này.", ErrorCodes.PermissionDenied);
        }

        var availableCategories = await dbContext.Categories
            .AsNoTracking()
            .Select(c => new CategoryCandidateDto(c.Id, c.Name, c.Slug))
            .ToListAsync(cancellationToken);

        if (availableCategories.Count == 0)
        {
            return new SuggestCategoriesResponseDto([]);
        }

        var suggestions = await aiService.SuggestCategoriesAsync(
            request.Title,
            request.ContentSnippet,
            availableCategories,
            cancellationToken);

        return new SuggestCategoriesResponseDto(suggestions);
    }
}
