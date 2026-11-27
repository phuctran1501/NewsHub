namespace NewsHub.Application.Tests.Features.Ai.Services;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Infrastructure.Services;
using Xunit;

public class GeminiAiServiceTests
{
    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(handler(request));
        }
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_ShouldSendApiKeyInHeaderAndFallbackFromV1BetaToV1On404()
    {
        var expectedVector = Enumerable.Repeat(0.25f, 768).ToArray();
        var stubHandler = new StubHttpMessageHandler(req =>
        {
            if (req.RequestUri!.AbsolutePath.StartsWith("/v1beta/", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { embedding = new { values = expectedVector } })
            };
        });

        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-api-key",
            ApiVersion = "v1beta",
            EmbeddingModel = "gemini-embedding-001",
            TimeoutSeconds = 10
        });

        var service = new GeminiAiService(
            new HttpClient(stubHandler),
            options,
            NullLogger<GeminiAiService>.Instance);

        var result = await service.GenerateEmbeddingAsync(
            "tin tức công nghệ",
            NewsHub.Domain.Enums.EmbeddingTaskType.RetrievalDocument,
            "Tiêu đề bài báo",
            CancellationToken.None);

        Assert.Equal(768, result.Length);
        // Xác minh vector đã được chuẩn hóa L2 (||v|| ≈ 1)
        var norm = Math.Sqrt(result.Sum(v => (double)v * v));
        Assert.InRange(norm, 0.999, 1.001);
        Assert.Equal(2, stubHandler.Requests.Count);
        Assert.Contains("/v1beta/models/gemini-embedding-001:embedContent", stubHandler.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Contains("/v1/models/gemini-embedding-001:embedContent", stubHandler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.DoesNotContain("key=", stubHandler.Requests[0].RequestUri!.AbsoluteUri);
        Assert.Equal("test-api-key", stubHandler.Requests[0].Headers.GetValues("x-goog-api-key").Single());
        var sentPayload = await stubHandler.Requests[0].Content!.ReadAsStringAsync();
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(sentPayload);
        Assert.Equal("RETRIEVAL_DOCUMENT", jsonDoc.RootElement.GetProperty("taskType").GetString());
        Assert.Equal("Tiêu đề bài báo", jsonDoc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenBothVersionsReturnNotFound_ShouldThrowServiceUnavailable503()
    {
        var stubHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":{\"message\":\"Not found\"}}")
        });

        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-api-key",
            ApiVersion = "v1beta",
            EmbeddingModel = "gemini-embedding-001",
            TimeoutSeconds = 10
        });

        var service = new GeminiAiService(
            new HttpClient(stubHandler),
            options,
            NullLogger<GeminiAiService>.Instance);

        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() =>
            service.GenerateEmbeddingAsync("test", CancellationToken.None));

        Assert.Equal(AppErrorKind.ServiceUnavailable, ex.ErrorKind);
        Assert.Equal(ErrorCodes.AiServiceUnavailable, ex.ErrorCode);
    }
}
