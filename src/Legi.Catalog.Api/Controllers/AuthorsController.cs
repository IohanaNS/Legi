using Legi.Catalog.Application.Authors.Queries.GetPopularAuthors;
using Legi.Catalog.Application.Authors.Queries.SearchAuthors;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/authors")]
public class AuthorsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthorsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search authors by name prefix for autocomplete
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchAuthorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchAuthorsResponse>> SearchAuthors(
        [FromQuery] string searchTerm,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchAuthorsQuery(searchTerm, limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the most popular authors by book count
    /// </summary>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(SearchAuthorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchAuthorsResponse>> GetPopularAuthors(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPopularAuthorsQuery(limit);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
