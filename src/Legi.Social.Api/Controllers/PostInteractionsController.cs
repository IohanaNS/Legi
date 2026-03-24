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
[Route("api/v1/social/posts/{postId:guid}")]
public class PostInteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostInteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpPost("likes")]
    [Authorize]
    public async Task<IActionResult> LikePost(Guid postId)
    {
        var userId = GetUserId();
        var command = new LikeContentCommand(userId, InteractableType.Post, postId);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/posts/{postId}/likes", result);
    }

    [HttpDelete("likes")]
    [Authorize]
    public async Task<IActionResult> UnlikePost(Guid postId)
    {
        var userId = GetUserId();
        var command = new UnlikeContentCommand(userId, InteractableType.Post, postId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetContentCommentsQuery(InteractableType.Post, postId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(Guid postId, [FromBody] CreateCommentRequest request)
    {
        var userId = GetUserId();
        var command = new CreateCommentCommand(userId, InteractableType.Post, postId, request.Content);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/comments/{result.CommentId}", result);
    }
}

public record CreateCommentRequest(string Content);
