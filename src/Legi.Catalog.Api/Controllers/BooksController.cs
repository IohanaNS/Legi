using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/books")]
public class BooksController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new book in the global catalog
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateBookResponse>> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        // TODO: Get authenticated user ID from JWT claims
        // For now, using a placeholder
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var command = new CreateBookCommand(
            request.Isbn,
            request.Title,
            request.Authors,
            userId,
            request.Synopsis,
            request.PageCount,
            request.Publisher,
            request.CoverUrl,
            request.Tags
        );

        var result = await mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}

// Request DTO
public record CreateBookRequest(
    string Isbn,
    string Title,
    List<string> Authors,
    string? Synopsis = null,
    int? PageCount = null,
    string? Publisher = null,
    string? CoverUrl = null,
    List<string>? Tags = null
);