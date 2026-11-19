namespace NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp;

using FluentValidation;
using MediatR;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using Pgvector;

public sealed record SearchArticlesByNlpQuery(
    string Query,
    int Limit = 10) : IRequest<IReadOnlyList<SemanticSearchResultDto>>;

public sealed class SearchArticlesByNlpQueryValidator : AbstractValidator<SearchArticlesByNlpQuery>
{
    public SearchArticlesByNlpQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Câu truy vấn tìm kiếm không được để trống.")
            .MaximumLength(200).WithMessage("Câu truy vấn tìm kiếm không được vượt quá 200 ký tự.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20).WithMessage("Số lượng kết quả (limit) phải từ 1 đến 20.");
    }
}

public sealed class SearchArticlesByNlpQueryHandler(
    IApplicationDbContext dbContext,
    IAiService aiService) : IRequestHandler<SearchArticlesByNlpQuery, IReadOnlyList<SemanticSearchResultDto>>
{
    public async Task<IReadOnlyList<SemanticSearchResultDto>> Handle(
        SearchArticlesByNlpQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Chuyển đổi câu query tự nhiên thành vector nhúng 768 chiều
        var queryEmbedding = await aiService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var queryVector = new Vector(queryEmbedding);

        // 2. Tìm kiếm theo khoảng cách Cosine trên PostgreSQL pgvector
        return await dbContext.SearchArticlesByEmbeddingAsync(
            queryVector,
            request.Limit,
            0.65,
            cancellationToken);
    }
}
