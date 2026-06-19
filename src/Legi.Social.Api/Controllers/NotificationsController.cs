using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Notifications.Commands.MarkAllNotificationsAsRead;
using Legi.Social.Application.Notifications.Commands.MarkNotificationAsRead;
using Legi.Social.Application.Notifications.Queries.GetNotifications;
using Legi.Social.Application.Notifications.Queries.GetUnreadCount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet("")]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetNotificationsQuery(GetUserId(), page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _mediator.Send(new GetUnreadCountQuery(GetUserId()));
        return Ok(new { count });
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        await _mediator.Send(new MarkNotificationAsReadCommand(GetUserId(), notificationId));
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _mediator.Send(new MarkAllNotificationsAsReadCommand(GetUserId()));
        return NoContent();
    }
}
