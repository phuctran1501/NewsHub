namespace NewsHub.Application.Tests.Services;

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Infrastructure.Services;
using Xunit;

public class GeminiAiServiceTests
{
    private readonly Mock<IOptions<GeminiOptions>> _optionsMock = new();
    private readonly Mock<ILogger<GeminiAiService>> _loggerMock = new();
    private readonly GeminiOptions _options = new()
    {
        ApiKey = "test-gemini-key",
        ModelName = "gemini-3.5-flash-lite",
        EmbeddingModel = "text-embedding-004",
        TimeoutSeconds = 10
    };

    public GeminiAiServiceTests()
    {
        _optionsMock.Setup(o => o.Value).Returns(_options);
    }

    private GeminiAiService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new GeminiAiService(httpClient, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SuggestCategoriesAsync_ShouldFilterOutHallucinatedCategories()
    {
        // Arrange
        var validCatId = Guid.NewGuid();
        var hallucinatedCatId = Guid.NewGuid();

        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    suggestions = new[]
                                    {
                                        new { categoryId = validCatId.ToString(), categoryName = "Kinh doanh", slug = "kinh-doanh", confidence = 0.95, reason = "Nội dung tài chính" },
                                        new { categoryId = hallucinatedCatId.ToString(), categoryName = "Thể thao ảo", slug = "the-thao-ao", confidence = 0.88, reason = "Không có trong DB" }
                                    }
                                })
                            }
                        }
                    }
                }
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);
        var availableCategories = new List<CategoryCandidateDto>
        {
            new(validCatId, "Kinh doanh", "kinh-doanh")
        };

        // Act
        var result = await service.SuggestCategoriesAsync("Tiêu đề", "Nội dung", availableCategories, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(validCatId, result[0].CategoryId);
        Assert.Equal("Kinh doanh", result[0].CategoryName);
    }

    [Fact]
    public async Task GenerateSummaryAndSentimentAsync_WhenGeminiReturnsValidJson_ShouldParseSuccessfully()
    {
        // Arrange
        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    bulletPoints = new[] { "Ý chính 1", "Ý chính 2" },
                                    sentiment = "Positive",
                                    extractedTags = new[] { "tag1", "tag2" },
                                    confidenceScore = 0.95
                                })
                            }
                        }
                    }
                }
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);

        // Act
        var result = await service.GenerateSummaryAndSentimentAsync("Tiêu đề", "Nội dung", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BulletPoints.Count);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal(2, result.ExtractedTags.Count);
        Assert.Equal(0.95f, result.ConfidenceScore);
    }

    [Fact]
    public async Task GenerateSummaryAndSentimentAsync_WhenSentimentIsUnknown_ShouldFallbackToNeutral()
    {
        // Arrange
        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = JsonSerializer.Serialize(new
                                {
                                    bulletPoints = new[] { "Ý chính 1" },
                                    sentiment = "UnknownOrInvalidSentiment",
                                    extractedTags = new[] { "tag1" },
                                    confidenceScore = 0.8
                                })
                            }
                        }
                    }
                }
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);

        // Act
        var result = await service.GenerateSummaryAndSentimentAsync("Tiêu đề", "Nội dung", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Neutral", result.Sentiment);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenGeminiReturnsEmbedding_ShouldReturnFloatArray()
    {
        // Arrange
        var values = new float[768];
        values[0] = 0.42f;

        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            embedding = new
            {
                values
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);

        // Act
        var result = await service.GenerateEmbeddingAsync("Văn bản thử nghiệm", CancellationToken.None);

        // Assert (vector được chuẩn hóa L2 nên [0.42, 0, ...] -> [1.0, 0, ...])
        Assert.NotNull(result);
        Assert.Equal(768, result.Length);
        Assert.InRange(result[0], 0.999f, 1.001f);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhenGeminiReturnsInvalidDimensions_ShouldThrowServiceUnavailableException()
    {
        // Arrange (giả lập trường hợp model trả về 3072 chiều thay vì 768)
        var values = new float[3072];

        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            embedding = new
            {
                values
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ServiceUnavailableException>(() => service.GenerateEmbeddingAsync("Văn bản thử nghiệm", CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidAiResponse, ex.ErrorCode);
    }

    [Fact]
    public async Task GenerateSummaryAndSentimentAsync_WhenGeminiReturnsJsonWrappedInMarkdownFences_ShouldParseSuccessfully()
    {
        // Arrange (giả lập trường hợp model bọc JSON trong ```json ... ```)
        var rawJson = JsonSerializer.Serialize(new
        {
            bulletPoints = new[] { "Ý chính 1", "Ý chính 2" },
            sentiment = "Positive",
            extractedTags = new[] { "tag1" },
            confidenceScore = 0.9
        });
        var markdownWrappedText = $"```json\n{rawJson}\n```";

        var geminiResponseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = markdownWrappedText
                            }
                        }
                    }
                }
            }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson)
            });

        var service = CreateService(handlerMock.Object);

        // Act
        var result = await service.GenerateSummaryAndSentimentAsync("Tiêu đề", "Nội dung", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BulletPoints.Count);
        Assert.Equal("Positive", result.Sentiment);
    }
}
