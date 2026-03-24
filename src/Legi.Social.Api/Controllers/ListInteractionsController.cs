using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Comments.Commands.CreateComment;
using Legi.Social.Application.Comments.Queries.GetContentComments;
using Legi.Social.Application.Likes.Commands.LikeContent;
using Legi.Social.Application.Likes.Commands.UnlikeContent;
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
