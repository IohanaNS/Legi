using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Comments.Commands.CreateComment;
using Legi.Social.Application.Comments.Queries.GetContentComments;
using Legi.Social.Application.Likes.Commands.LikeContent;
using Legi.Social.Application.Likes.Commands.UnlikeContent;
using Legi.Social.Application.Lists.Commands.FollowList;
using Legi.Social.Application.Lists.Commands.UnfollowList;
using Legi.Social.Application.Lists.Queries.GetListSocialState;
using Legi.Social.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social/lists/{listId:guid}")]
public class ListInteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ListInteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    private Guid? GetUserIdOrNull()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>
    /// Live social state of a list for the current viewer (counts + like/follow flags).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetSocialState(Guid listId)
    {
        var query = new GetListSocialStateQuery(listId, GetUserIdOrNull());
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("follows")]
    [Authorize]
    public async Task<IActionResult> FollowList(Guid listId)
    {
        var userId = GetUserId();
        await _mediator.Send(new FollowListCommand(userId, listId));
        return Created($"/api/v1/social/lists/{listId}/follows", null);
    }

    [HttpDelete("follows")]
    [Authorize]
    public async Task<IActionResult> UnfollowList(Guid listId)
    {
        var userId = GetUserId();
        await _mediator.Send(new UnfollowListCommand(userId, listId));
        return NoContent();
    }

    [HttpPost("likes")]
    [Authorize]
    public async Task<IActionResult> LikeList(Guid listId)
    {
        var userId = GetUserId();
        var command = new LikeContentCommand(userId, InteractableType.List, listId);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/lists/{listId}/likes", result);
    }

    [HttpDelete("likes")]
    [Authorize]
    public async Task<IActionResult> UnlikeList(Guid listId)
    {
        var userId = GetUserId();
        var command = new UnlikeContentCommand(userId, InteractableType.List, listId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid listId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetContentCommentsQuery(InteractableType.List, listId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(Guid listId, [FromBody] CreateCommentRequest request)
    {
        var userId = GetUserId();
        var command = new CreateCommentCommand(userId, InteractableType.List, listId, request.Content);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/comments/{result.CommentId}", result);
    }
}
