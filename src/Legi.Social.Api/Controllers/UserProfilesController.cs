using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Lists.Queries.GetFollowedLists;
using Legi.Social.Application.Profiles.Queries.GetUserProfile;
using Legi.Social.Application.Profiles.Queries.SearchUsers;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Social.Api.Controllers;

[ApiController]
[Route("api/v1/social/users")]
public class UserProfilesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserProfilesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string usernamePrefix,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var viewerUserId = GetViewerUserIdOrNull();

        var query = new SearchUsersQuery(usernamePrefix, viewerUserId, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var viewerUserId = GetViewerUserIdOrNull();

        var query = new GetUserProfileQuery(userId, viewerUserId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}/followed-lists")]
    public async Task<IActionResult> GetFollowedLists(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFollowedListsQuery(userId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private Guid? GetViewerUserIdOrNull()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
