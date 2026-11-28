namespace NewsHub.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;

[ApiController]
[Route("api/v1/public")]
public class PublicMarketController(
    IMarketTickerService marketTickerService,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách chỉ số thị trường Việt Nam (VN-INDEX, VN30, HNX-INDEX, UPCOM, USD/VND, SJC 9999).
    /// Dữ liệu được đệm Redis 10 phút kèm fallback an toàn khi nguồn ngoài gián đoạn.
    /// </summary>
    [HttpGet("market-ticker")]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetMarketTicker(CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await marketTickerService.GetMarketTickerAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }
}
