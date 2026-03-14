using Legi.Library.Application.ReadingPosts.Commands.CreateReadingPost;
using Legi.Library.Application.ReadingPosts.Commands.DeleteReadingPost;
using Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;
using Legi.Library.Application.ReadingPosts.Queries.GetUserBookPosts;
using Legi.Library.Domain.Enums;
using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Library.Api.Controllers;

[ApiController]
[Route("api/v1/library")]
[Authorize]
public class ReadingPostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReadingPostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Get posts for a specific user book.
    /// </summary>
    [HttpGet("{userBookId:guid}/posts")]
    public async Task<IActionResult> GetUserBookPosts(
        Guid userBookId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserBookPostsQuery(userBookId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a reading post for a user book.
    /// </summary>
    [HttpPost("{userBookId:guid}/posts")]
    public async Task<IActionResult> CreateReadingPost(
        Guid userBookId,
        [FromBody] CreateReadingPostRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateReadingPostCommand(
            userBookId,
            GetUserId(),
            request.Content,
            request.ProgressValue,
            request.ProgressType,
            request.ReadingDate);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetUserBookPosts),
            new { userBookId },
            result);
    }

    /// <summary>
    /// Update a reading post.
    /// </summary>
    [HttpPut("posts/{postId:guid}")]
    public async Task<IActionResult> UpdateReadingPost(
        Guid postId,
        [FromBody] UpdateReadingPostRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReadingPostCommand(
            postId,
            GetUserId(),
            request.Content,
            request.ProgressValue,
            request.ProgressType);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a reading post.
    /// </summary>
    [HttpDelete("posts/{postId:guid}")]
    public async Task<IActionResult> DeleteReadingPost(
        Guid postId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteReadingPostCommand(postId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

// Request DTOs
public record CreateReadingPostRequest(
    string? Content,
    int? ProgressValue = null,
    ProgressType? ProgressType = null,
    DateOnly? ReadingDate = null);

public record UpdateReadingPostRequest(
    string? Content,
    int? ProgressValue = null,
    ProgressType? ProgressType = null);
