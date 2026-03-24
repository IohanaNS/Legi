using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Follows.Commands.FollowUser;
using Legi.Social.Application.Follows.Commands.UnfollowUser;
using Legi.Social.Application.Follows.Queries.GetFollowers;
using Legi.Social.Application.Follows.Queries.GetFollowing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social")]
public class FollowsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FollowsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    [HttpPost("follows")]
    [Authorize]
    public async Task<IActionResult> Follow([FromBody] FollowRequest request)
    {
        var userId = GetUserId();
        var command = new FollowUserCommand(userId, request.FollowingId);
        var result = await _mediator.Send(command);
        return Created($"/api/v1/social/follows/{request.FollowingId}", result);
    }

    [HttpDelete("follows/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> Unfollow(Guid userId)
    {
        var currentUserId = GetUserId();
        var command = new UnfollowUserCommand(currentUserId, userId);
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("users/{userId:guid}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowers(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Guid? viewerUserId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;
        var query = new GetFollowersQuery(userId, viewerUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFollowing(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        Guid? viewerUserId = User.Identity?.IsAuthenticated == true ? GetUserId() : null;
        var query = new GetFollowingQuery(userId, viewerUserId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public record FollowRequest(Guid FollowingId);
