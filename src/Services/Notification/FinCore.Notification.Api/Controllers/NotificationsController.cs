using FinCore.BuildingBlocks.Responses;
using FinCore.BuildingBlocks.Security;
using FinCore.Notification.Application.DTOs;
using FinCore.Notification.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Notification.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool unreadOnly = false)
    {
        var result = await _service.GetMineAsync(User.GetUserId(), page, pageSize, unreadOnly);
        return Ok(ApiResponse<PagedResult<NotificationResponse>>.Ok(result));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _service.GetUnreadCountAsync(User.GetUserId());
        return Ok(ApiResponse<object>.Ok(new { count }));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        await _service.MarkAsReadAsync(User.GetUserId(), id);
        return Ok(ApiResponse<object?>.Ok(null, "Notification marked as read."));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _service.MarkAllAsReadAsync(User.GetUserId());
        return Ok(ApiResponse<object?>.Ok(null, "All notifications marked as read."));
    }
}