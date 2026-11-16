namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Media.Commands.DeleteMediaImage;
using NewsHub.Application.Features.Media.Commands.UploadMediaImage;
using NewsHub.Application.Features.Media.Queries.GetMyMediaFiles;
using NewsHub.Domain.Enums;

[ApiController]
[Authorize]
[Route("api/v1/media")]
public class MediaController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Tải tệp tin hình ảnh lên Cloudinary và lưu thông tin vào cơ sở dữ liệu.
    /// Hỗ trợ Avatar, ArticleCover, ArticleBody. Tối đa 5MB, định dạng .jpg, .jpeg, .png, .webp.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] MediaFolder folder,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();

        if (file is null)
        {
            var invalidCommand = new UploadMediaImageCommand(
                Stream.Null,
                string.Empty,
                0,
                string.Empty,
                folder);

            var invalidResult = await mediator.Send(invalidCommand, cancellationToken);
            return Created($"/api/v1/media/{invalidResult.Id}", ApiResponseFactory.Success(invalidResult, correlationId));
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadMediaImageCommand(
            stream,
            file.FileName,
            file.Length,
            file.ContentType,
            folder);

        var result = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/media/{result.Id}", ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xóa tệp tin hình ảnh trên Cloudinary và trong cơ sở dữ liệu.
    /// Yêu cầu là người tải lên hoặc tài khoản Admin. Không cho phép xóa nếu ảnh đang được dùng làm ảnh đại diện.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DeleteMediaImageCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa ảnh thành công." }, correlationId));
    }

    /// <summary>
    /// Lấy danh sách tệp tin hình ảnh do người dùng hiện tại đã tải lên, có phân trang và lọc theo thư mục.
    /// </summary>
    [HttpGet("my-media")]
    public async Task<IActionResult> GetMyMedia(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] MediaFolder? folder = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(
            new GetMyMediaFilesQuery(pageNumber, pageSize, folder),
            cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }
}
