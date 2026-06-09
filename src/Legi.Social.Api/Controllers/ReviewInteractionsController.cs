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
[Route("api/v1/social/reviews/{reviewId:guid}")]
public class ReviewInteractionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewInteractionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpPost("likes")]
    [Authorize]
    public async Task<IActionResult> LikeReview(Guid reviewId)
    {
        var userId = GetUserId();
        var command = new LikeContentCommand(userId, InteractableType.Review, reviewId);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/reviews/{reviewId}/likes", result);
    }

    [HttpDelete("likes")]
    [Authorize]
    public async Task<IActionResult> UnlikeReview(Guid reviewId)
    {
        var userId = GetUserId();
        var command = new UnlikeContentCommand(userId, InteractableType.Review, reviewId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid reviewId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetContentCommentsQuery(InteractableType.Review, reviewId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("comments")]
    [Authorize]
    public async Task<IActionResult> CreateComment(Guid reviewId, [FromBody] CreateReviewCommentRequest request)
    {
        var userId = GetUserId();
        var command = new CreateCommentCommand(userId, InteractableType.Review, reviewId, request.Content);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/comments/{result.CommentId}", result);
    }
}

public record CreateReviewCommentRequest(string Content);
