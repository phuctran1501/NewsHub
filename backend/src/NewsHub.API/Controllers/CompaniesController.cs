namespace NewsHub.API.Controllers;

using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.Commands.CreateCompany;
using NewsHub.Application.Features.Companies.Commands.DissolveCompany;
using NewsHub.Application.Features.Companies.Commands.FollowCompany;
using NewsHub.Application.Features.Companies.Commands.InviteJournalist;
using NewsHub.Application.Features.Companies.Commands.LeaveCompany;
using NewsHub.Application.Features.Companies.Commands.RemoveCompanyMember;
using NewsHub.Application.Features.Companies.Commands.RequestJoinCompany;
using NewsHub.Application.Features.Companies.Commands.RespondToInvitation;
using NewsHub.Application.Features.Companies.Commands.RespondToJoinRequest;
using NewsHub.Application.Features.Companies.Commands.ReviewEditorialArticle;
using NewsHub.Application.Features.Companies.Commands.TransferOwnership;
using NewsHub.Application.Features.Companies.Commands.UnfollowCompany;
using NewsHub.Application.Features.Companies.Queries.GetCompanyInternalArticles;
using NewsHub.Application.Features.Companies.Queries.GetCompanyJoinRequests;
using NewsHub.Application.Features.Companies.Queries.GetCompanyMembers;
using NewsHub.Application.Features.Companies.Queries.GetFollowedCompanies;
using NewsHub.Application.Features.Companies.Queries.GetMyCompanies;
using NewsHub.Application.Features.Companies.Queries.GetMyInvitations;
using NewsHub.Application.Features.Companies.Queries.GetPendingEditorialArticles;
using NewsHub.Application.Features.Companies.Queries.SearchCompanies;
using NewsHub.Application.Features.Companies.Queries.SearchJournalistByEmail;

public sealed record CreateCompanyRequest(
    [property: JsonRequired] string Name,
    [property: JsonRequired] string TaxCode);

public sealed record RespondInvitationRequest(
    [property: JsonRequired] bool Accept);

public sealed record InviteJournalistRequest(
    [property: JsonRequired] Guid UserId);

public sealed record RespondJoinRequest(
    [property: JsonRequired] bool Approve);

public sealed record TransferOwnershipRequest(
    [property: JsonRequired] Guid NewOwnerId);

public sealed record ReviewEditorialArticleRequest(
    [property: JsonRequired] string Action,
    string? EditorialNotes = null);

/// <summary>
/// Quản lý Tòa soạn & Công ty Truyền thông (Multi-Tenant Press & Publishing Platform).
/// </summary>
[ApiController]
[Route("api/v1/companies")]
public class CompaniesController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    // ==========================================
    // 1. COMPANY LIFECYCLE & DISCOVERY
    // ==========================================

    /// <summary>
    /// Đăng ký thành lập công ty / tòa soạn mới (Chờ Admin thẩm định).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new CreateCompanyCommand(request.Name, request.TaxCode);
        var result = await mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Tìm kiếm công ty / tòa soạn đã được phê duyệt theo Tên hoặc Mã định danh 8 số.
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> SearchCompanies(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new SearchCompaniesQuery(searchTerm, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Xem danh sách các công ty nhà báo hiện đang là thành viên (Tối đa 5 công ty).
    /// </summary>
    [HttpGet("my-companies")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetMyCompanies(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetMyCompaniesQuery(), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Rời khỏi công ty (Chỉ dành cho Member; các bài nháp và chờ duyệt sẽ được chuyển về cá nhân).
    /// </summary>
    [HttpPost("{companyId:guid}/leave")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> LeaveCompany(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new LeaveCompanyCommand(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã rời khỏi tòa soạn thành công. Các bài viết nháp và chờ duyệt đã được chuyển về cá nhân.", correlationId));
    }

    /// <summary>
    /// Giải thể / Đóng tòa soạn (Chỉ khi công ty chỉ còn duy nhất 1 mình Owner).
    /// </summary>
    [HttpDelete("{companyId:guid}")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> DissolveCompany(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DissolveCompanyCommand(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã giải thể tòa soạn thành công.", correlationId));
    }

    // ==========================================
    // 2. RECRUITMENT, INVITATIONS & JOIN REQUESTS
    // ==========================================

    /// <summary>
    /// Gửi đơn xin gia nhập tòa soạn.
    /// </summary>
    [HttpPost("{companyId:guid}/join-requests")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> RequestJoinCompany(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new RequestJoinCompanyCommand(companyId), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xem danh sách lời mời gia nhập gửi tới nhà báo hiện tại.
    /// </summary>
    [HttpGet("my-invitations")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetMyInvitations(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetMyInvitationsQuery(), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Phản hồi lời mời gia nhập (Chấp nhận hoặc Từ chối).
    /// </summary>
    [HttpPost("invitations/{requestId:guid}/respond")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> RespondToInvitation(
        [FromRoute] Guid requestId,
        [FromBody] RespondInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new RespondToInvitationCommand(requestId, request.Accept), cancellationToken);

        var message = request.Accept ? "Đã chấp nhận lời mời gia nhập tòa soạn." : "Đã từ chối lời mời gia nhập tòa soạn.";
        return Ok(ApiResponseFactory.Success(message, correlationId));
    }

    /// <summary>
    /// Tìm kiếm thông tin nhà báo qua email trước khi bấm gửi lời mời (Chỉ dành cho Owner).
    /// </summary>
    [HttpGet("{companyId:guid}/search-journalists")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> SearchJournalist(
        [FromRoute] Guid companyId,
        [FromQuery] string email,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new SearchJournalistByEmailQuery(companyId, email), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Mời nhà báo vào tòa soạn (Chỉ dành cho Owner).
    /// </summary>
    [HttpPost("{companyId:guid}/invitations")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> InviteJournalist(
        [FromRoute] Guid companyId,
        [FromBody] InviteJournalistRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new InviteJournalistCommand(companyId, request.UserId), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xem danh sách đơn xin gia nhập tòa soạn (Chỉ dành cho Owner).
    /// </summary>
    [HttpGet("{companyId:guid}/join-requests")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetJoinRequests(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetCompanyJoinRequestsQuery(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Duyệt hoặc từ chối đơn xin gia nhập tòa soạn (Chỉ dành cho Owner).
    /// </summary>
    [HttpPost("{companyId:guid}/join-requests/{requestId:guid}/respond")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> RespondToJoinRequest(
        [FromRoute] Guid companyId,
        [FromRoute] Guid requestId,
        [FromBody] RespondJoinRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new RespondToJoinRequestCommand(companyId, requestId, request.Approve), cancellationToken);

        var message = request.Approve ? "Đã duyệt đơn gia nhập thành công." : "Đã từ chối đơn gia nhập.";
        return Ok(ApiResponseFactory.Success(message, correlationId));
    }

    // ==========================================
    // 3. MEMBER MANAGEMENT & OWNERSHIP
    // ==========================================

    /// <summary>
    /// Xem danh sách nhân viên trong tòa soạn (Chỉ dành cho Owner).
    /// </summary>
    [HttpGet("{companyId:guid}/members")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetMembers(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetCompanyMembersQuery(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xóa một thành viên khỏi tòa soạn (Chỉ dành cho Owner; giải phóng bài nháp/chờ duyệt về cá nhân).
    /// </summary>
    [HttpDelete("{companyId:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] Guid companyId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new RemoveCompanyMemberCommand(companyId, userId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã xóa thành viên khỏi tòa soạn thành công.", correlationId));
    }

    /// <summary>
    /// Chuyển giao quyền sở hữu tòa soạn (Owner) cho thành viên khác.
    /// </summary>
    [HttpPost("{companyId:guid}/transfer-ownership")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> TransferOwnership(
        [FromRoute] Guid companyId,
        [FromBody] TransferOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new TransferOwnershipCommand(companyId, request.NewOwnerId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã chuyển giao quyền sở hữu tòa soạn thành công.", correlationId));
    }

    // ==========================================
    // 4. EDITORIAL WORKFLOW & INTERNAL ARTICLES
    // ==========================================

    /// <summary>
    /// Xem kho bài viết nội bộ trong tòa soạn (Read-only cho cả Owner và Member).
    /// </summary>
    [HttpGet("{companyId:guid}/articles")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetInternalArticles(
        [FromRoute] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetCompanyInternalArticlesQuery(companyId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Xem danh sách bài viết chờ tòa soạn phê duyệt (Chỉ dành cho Owner).
    /// </summary>
    [HttpGet("{companyId:guid}/pending-articles")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> GetPendingArticles(
        [FromRoute] Guid companyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetPendingEditorialArticlesQuery(companyId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Kiểm duyệt bài viết của tòa soạn (Duyệt xuất bản / Yêu cầu sửa / Từ chối).
    /// </summary>
    [HttpPost("{companyId:guid}/articles/{articleId:guid}/review")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> ReviewArticle(
        [FromRoute] Guid companyId,
        [FromRoute] Guid articleId,
        [FromBody] ReviewEditorialArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new ReviewEditorialArticleCommand(companyId, articleId, request.Action, request.EditorialNotes);
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã hoàn tất xử lý biên tập bài viết thành công.", correlationId));
    }

    // ==========================================
    // 5. COMPANY FOLLOWING NETWORK
    // ==========================================

    /// <summary>
    /// Theo dõi Tòa soạn (Dành cho Độc giả và Nhà báo bên ngoài; thành viên nội bộ không được theo dõi).
    /// </summary>
    [HttpPost("{companyId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> FollowCompany(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new FollowCompanyCommand(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã theo dõi Tòa soạn thành công.", correlationId));
    }

    /// <summary>
    /// Bỏ theo dõi Tòa soạn.
    /// </summary>
    [HttpDelete("{companyId:guid}/follow")]
    [Authorize]
    public async Task<IActionResult> UnfollowCompany(
        [FromRoute] Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new UnfollowCompanyCommand(companyId), cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã bỏ theo dõi Tòa soạn.", correlationId));
    }

    /// <summary>
    /// Xem danh sách Tòa soạn đang theo dõi.
    /// </summary>
    [HttpGet("following")]
    [Authorize]
    public async Task<IActionResult> GetFollowedCompanies(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetFollowedCompaniesQuery(pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }
}
