namespace NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Enums;
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

public sealed partial class SearchArticlesByNlpQueryHandler(
    IApplicationDbContext dbContext,
    IAiService aiService,
    ICacheService? cacheService = null) : IRequestHandler<SearchArticlesByNlpQuery, IReadOnlyList<SemanticSearchResultDto>>
{
    public const double DefaultMinSimilarity = 0.58;
    private const float HighConfidenceTopScore = 0.70f;
    private const float StrongMatchFloor = 0.63f;
    private const float MaxRelativeDrop = 0.14f;
    private static readonly TimeSpan QueryEmbeddingCacheTtl = TimeSpan.FromMinutes(10);

    private static readonly HashSet<string> VietnameseStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "của", "và", "các", "cho", "trong", "là", "về", "đến", "với", "những",
        "một", "được", "khi", "tại", "từ", "có", "không", "đang", "đã", "sẽ",
        "này", "đó", "theo", "như", "để", "bị", "bởi", "hay", "hoặc", "nhưng",
        "vì", "nên", "thì", "trên", "dưới", "qua", "lại", "như", "thế", "nào",
        "gì", "sao", "cách", "làm", "nhất", "hơn", "rất", "cùng"
    };

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex NonWordSplitRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    public async Task<IReadOnlyList<SemanticSearchResultDto>> Handle(
        SearchArticlesByNlpQuery request,
        CancellationToken cancellationToken)
    {
        var cleanQuery = request.Query.Trim();
        var lowerQuery = cleanQuery.ToLowerInvariant();
        var candidateLimit = Math.Clamp(request.Limit * 2, 10, 30);

        // 1. Chuyển đổi câu query tự nhiên thành vector nhúng 768 chiều (RETRIEVAL_QUERY + Redis cache 10p)
        var queryEmbedding = await GetOrCreateQueryEmbeddingAsync(cleanQuery, lowerQuery, cancellationToken);
        var queryVector = new Vector(queryEmbedding);

        // 2. Kênh 1 — Tìm kiếm ngữ nghĩa theo khoảng cách Cosine trên PostgreSQL pgvector
        var semanticMatches = await dbContext.SearchArticlesByEmbeddingAsync(
            queryVector,
            candidateLimit,
            DefaultMinSimilarity,
            cancellationToken);

        // 3. Kênh 2 — Tìm kiếm từ khóa (Lexical Match trên Tiêu đề & Nội dung bài đã xuất bản)
        var lexicalMatches = await SearchLexicalCandidatesAsync(lowerQuery, candidateLimit, cancellationToken);

        // 4. Hợp nhất Hybrid Search (Semantic + Lexical Boost + Lọc nhiễu đuôi động)
        return FuseHybridResults(semanticMatches, lexicalMatches, request.Limit);
    }

    private async Task<float[]> GetOrCreateQueryEmbeddingAsync(
        string cleanQuery,
        string lowerQuery,
        CancellationToken cancellationToken)
    {
        if (cacheService is null)
        {
            return await aiService.GenerateEmbeddingAsync(
                cleanQuery,
                EmbeddingTaskType.RetrievalQuery,
                null,
                cancellationToken);
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(lowerQuery));
        var cacheKey = $"ai:search:emb:{Convert.ToHexStringLower(hashBytes)[..24]}";

        return await cacheService.GetOrCreateAsync(
            cacheKey,
            ct => aiService.GenerateEmbeddingAsync(
                cleanQuery,
                EmbeddingTaskType.RetrievalQuery,
                null,
                ct),
            QueryEmbeddingCacheTtl,
            cancellationToken);
    }

    private async Task<List<SemanticSearchResultDto>> SearchLexicalCandidatesAsync(
        string lowerQuery,
        int candidateLimit,
        CancellationToken cancellationToken)
    {
        if (dbContext.Articles is null)
        {
            return [];
        }

        var significantTokens = ExtractSignificantTokens(lowerQuery);
        var primaryTokens = significantTokens
            .OrderByDescending(t => t.Length)
            .Take(3)
            .ToList();

        var baseQuery = dbContext.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published);

        // Lọc ứng viên ở DB bằng cụm chính xác hoặc bất kỳ từ khóa chính nào
        var t0 = primaryTokens.Count > 0 ? primaryTokens[0] : null;
        var t1 = primaryTokens.Count > 1 ? primaryTokens[1] : null;
        var t2 = primaryTokens.Count > 2 ? primaryTokens[2] : null;

        var filtered = baseQuery.Where(a =>
            a.Title.ToLower().Contains(lowerQuery) ||
            a.ContentHtml.ToLower().Contains(lowerQuery) ||
            (t0 != null && a.Title.ToLower().Contains(t0)) ||
            (t1 != null && a.Title.ToLower().Contains(t1)) ||
            (t2 != null && a.Title.ToLower().Contains(t2)));

        var rawCandidates = await filtered
            .OrderByDescending(a => a.PublishedAt)
            .Take(candidateLimit * 2)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Slug,
                a.ContentHtml,
                a.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var scored = new List<SemanticSearchResultDto>();
        foreach (var candidate in rawCandidates)
        {
            var score = ComputeLexicalScore(
                lowerQuery,
                significantTokens,
                candidate.Title,
                candidate.ContentHtml);

            if (score > 0f)
            {
                scored.Add(new SemanticSearchResultDto(
                    candidate.Id,
                    candidate.Title,
                    candidate.Slug,
                    score,
                    candidate.PublishedAt));
            }
        }

        return scored
            .OrderByDescending(x => x.SimilarityScore)
            .ThenByDescending(x => x.PublishedAt)
            .Take(candidateLimit)
            .ToList();
    }

    private static float ComputeLexicalScore(
        string lowerQuery,
        IReadOnlyList<string> significantTokens,
        string title,
        string contentHtml)
    {
        var lowerTitle = title.ToLowerInvariant();
        if (lowerTitle.Contains(lowerQuery, StringComparison.Ordinal))
        {
            return 0.85f;
        }

        var lowerPlainContent = HtmlTagRegex().Replace(contentHtml, " ").ToLowerInvariant();
        if (lowerPlainContent.Contains(lowerQuery, StringComparison.Ordinal))
        {
            return 0.70f;
        }

        if (significantTokens.Count == 0)
        {
            return 0f;
        }

        var titleHits = significantTokens.Count(token => lowerTitle.Contains(token, StringComparison.Ordinal));
        var titleCoverage = (float)titleHits / significantTokens.Count;

        if ((significantTokens.Count == 1 && titleHits == 1) ||
            (titleHits >= 2 && titleCoverage >= 0.5f))
        {
            return Math.Clamp(0.58f + (0.20f * titleCoverage), 0f, 0.80f);
        }

        var combinedHits = significantTokens.Count(token =>
            lowerTitle.Contains(token, StringComparison.Ordinal) ||
            lowerPlainContent.Contains(token, StringComparison.Ordinal));
        var combinedCoverage = (float)combinedHits / significantTokens.Count;

        if (combinedHits >= 2 && combinedCoverage >= 0.6f)
        {
            return Math.Clamp(0.50f + (0.15f * combinedCoverage), 0f, 0.66f);
        }

        return 0f;
    }

    private static IReadOnlyList<SemanticSearchResultDto> FuseHybridResults(
        IReadOnlyList<SemanticSearchResultDto> semanticMatches,
        IReadOnlyList<SemanticSearchResultDto> lexicalMatches,
        int limit)
    {
        var fused = new Dictionary<Guid, SemanticSearchResultDto>();

        foreach (var item in semanticMatches)
        {
            fused[item.ArticleId] = item with
            {
                SimilarityScore = Math.Clamp(item.SimilarityScore, 0f, 0.99f)
            };
        }

        foreach (var lex in lexicalMatches)
        {
            if (fused.TryGetValue(lex.ArticleId, out var sem))
            {
                // Có mặt ở cả Semantic và Lexical: cộng hưởng tăng điểm ưu tiên lên đầu
                var boosted = Math.Clamp(
                    Math.Max(sem.SimilarityScore, lex.SimilarityScore * 0.90f) + (0.12f * lex.SimilarityScore),
                    0f,
                    0.99f);

                fused[lex.ArticleId] = sem with { SimilarityScore = boosted };
            }
            else if (lex.SimilarityScore >= 0.60f)
            {
                // Khớp từ khóa mạnh (VD: cụm chính xác trong tiêu đề hoặc bài mới chưa kịp sinh embedding)
                fused[lex.ArticleId] = lex;
            }
        }

        var ranked = fused.Values
            .OrderByDescending(r => r.SimilarityScore)
            .ThenByDescending(r => r.PublishedAt)
            .ToList();

        if (ranked.Count == 0)
        {
            return ranked;
        }

        // Lọc nhiễu đuôi (tail noise): khi đã có kết quả rất khớp (>= 70%), loại các bài rớt điểm quá sâu so với kết quả tốt nhất
        var topScore = ranked[0].SimilarityScore;
        if (topScore >= HighConfidenceTopScore)
        {
            var dynamicFloor = Math.Max(
                (float)DefaultMinSimilarity,
                Math.Min(StrongMatchFloor, topScore - MaxRelativeDrop));

            ranked = ranked
                .Where(r => r.SimilarityScore >= dynamicFloor)
                .ToList();
        }

        return ranked.Take(limit).ToList();
    }

    private static List<string> ExtractSignificantTokens(string lowerQuery)
    {
        return NonWordSplitRegex()
            .Split(lowerQuery)
            .Where(token => token.Length >= 2 && !VietnameseStopwords.Contains(token))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
