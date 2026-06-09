using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Feed.Commands.DeleteFeedItem;
using Legi.Social.Application.Feed.Queries.GetBookReviews;
using Legi.Social.Application.Feed.Queries.GetFeed;
using Legi.Social.Application.Feed.Queries.GetUserActivity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social")]
public class FeedController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeedController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpGet("feed")]
    [Authorize]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetUserId();
        var query = new GetFeedQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpDelete("feed/{feedItemId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteFeedItem(Guid feedItemId)
    {
        var userId = GetUserId();
        var command = new DeleteFeedItemCommand(userId, feedItemId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("users/{userId:guid}/activity")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserActivity(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Guid? viewerUserId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;
        var query = new GetUserActivityQuery(userId, viewerUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("books/{bookId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBookReviews(Guid bookId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Guid? viewerUserId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;
        var query = new GetBookReviewsQuery(bookId, viewerUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
