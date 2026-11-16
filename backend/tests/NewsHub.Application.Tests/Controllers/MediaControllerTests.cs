namespace NewsHub.Application.Tests.Controllers;

using System.Text;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewsHub.API.Controllers;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Media.Commands.DeleteMediaImage;
using NewsHub.Application.Features.Media.Commands.UploadMediaImage;
using NewsHub.Application.Features.Media.DTOs;
using NewsHub.Application.Features.Media.Queries.GetMyMediaFiles;
using NewsHub.Domain.Enums;
using Xunit;

public class MediaControllerTests
{
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly Mock<ICorrelationIdProvider> _correlationIdProviderMock = new();
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        _correlationIdProviderMock.Setup(p => p.GetCorrelationId()).Returns("corr-123");
        _controller = new MediaController(_mediatorMock.Object, _correlationIdProviderMock.Object);
    }

    [Fact]
    public async Task Upload_WithValidFile_ShouldSendUploadCommandAndReturnCreated()
    {
        // Arrange
        var content = "fake-file-content";
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        formFileMock.Setup(f => f.FileName).Returns("photo.png");
        formFileMock.Setup(f => f.Length).Returns(bytes.Length);
        formFileMock.Setup(f => f.ContentType).Returns("image/png");

        var responseDto = new MediaFileResponseDto(
            Guid.NewGuid(),
            "pub/photo",
            "https://cdn.example.com/photo.webp",
            "photo.png",
            "webp",
            bytes.Length,
            600,
            400,
            MediaFolder.Avatar,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadMediaImageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Upload(formFileMock.Object, MediaFolder.Avatar, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal($"/api/v1/media/{responseDto.Id}", createdResult.Location);
        var envelope = Assert.IsType<ApiResponse<MediaFileResponseDto>>(createdResult.Value);
        Assert.Equal(responseDto, envelope.Data);
        Assert.Equal("corr-123", envelope.CorrelationId);
    }

    [Fact]
    public async Task Upload_WithNullFile_ShouldSendDefaultCommandAndReturnCreated()
    {
        // Arrange
        var responseDto = new MediaFileResponseDto(
            Guid.NewGuid(),
            "pub/photo",
            "https://cdn.example.com/photo.webp",
            "photo.png",
            "webp",
            100,
            600,
            400,
            MediaFolder.Avatar,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UploadMediaImageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.Upload(null, MediaFolder.Avatar, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var envelope = Assert.IsType<ApiResponse<MediaFileResponseDto>>(createdResult.Value);
        Assert.Equal(responseDto, envelope.Data);
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldSendDeleteCommandAndReturnOk()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteMediaImageCommand>(c => c.Id == mediaId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _controller.Delete(mediaId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        var correlationIdProp = okResult.Value.GetType().GetProperty("CorrelationId");
        Assert.NotNull(correlationIdProp);
        var correlationId = correlationIdProp.GetValue(okResult.Value) as string;
        Assert.Equal("corr-123", correlationId);
    }

    [Fact]
    public async Task GetMyMedia_WithParams_ShouldSendQueryAndReturnOkPaged()
    {
        // Arrange
        var items = new List<MediaFileResponseDto>
        {
            new(
                Guid.NewGuid(),
                "pub/1",
                "https://cdn.example.com/1.webp",
                "1.webp",
                "webp",
                500,
                800,
                600,
                MediaFolder.ArticleCover,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow)
        };
        var pagedList = new PagedList<MediaFileResponseDto>(items, 1, 12, 1);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetMyMediaFilesQuery>(q => q.PageNumber == 1 && q.PageSize == 12 && q.Folder == MediaFolder.ArticleCover), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        // Act
        var result = await _controller.GetMyMedia(1, 12, MediaFolder.ArticleCover, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<PagedApiResponse<MediaFileResponseDto>>(okResult.Value);
        Assert.Single(envelope.Data);
        Assert.Equal(1, envelope.Meta.PageNumber);
        Assert.Equal(12, envelope.Meta.PageSize);
        Assert.Equal(1, envelope.Meta.TotalCount);
        Assert.Equal("corr-123", envelope.CorrelationId);
    }
}
