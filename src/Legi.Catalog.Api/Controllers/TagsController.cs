using Legi.Catalog.Application.Tags.Queries.GetPopularTags;
using Legi.Catalog.Application.Tags.Queries.SearchTags;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/tags")]
public class TagsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search tags by name prefix for autocomplete
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchTagsResponse>> SearchTags(
        [FromQuery] string searchTerm,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchTagsQuery(searchTerm, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the most popular tags by usage count
    /// </summary>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(SearchTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchTagsResponse>> GetPopularTags(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPopularTagsQuery(limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
