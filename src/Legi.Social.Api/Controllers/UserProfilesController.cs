using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Profiles.Queries.GetUserProfile;
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

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        Guid? viewerUserId = User.Identity?.IsAuthenticated == true
            ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)
            : null;

        var query = new GetUserProfileQuery(userId, viewerUserId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
