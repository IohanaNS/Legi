using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Books.Commands.DeleteBook;
using Legi.Catalog.Application.Books.Commands.UpdateBook;
using Legi.Catalog.Application.Books.Queries.GetBookDetails;
using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/books")]
public class BooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public BooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Search books in the global catalog with filters and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SearchBooksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchBooksResponse>> SearchBooks(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? authorSlug = null,
        [FromQuery] string? tagSlug = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BookSortBy sortBy = BookSortBy.Relevance,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchBooksQuery(
            searchTerm,
            authorSlug,
            tagSlug,
            minRating,
            pageNumber,
            pageSize,
            sortBy,
            sortDescending
        );

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed information about a specific book
    /// </summary>
    [HttpGet("{bookId:guid}")]
    [ProducesResponseType(typeof(GetBookDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetBookDetailsResponse>> GetBookDetails(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var query = new GetBookDetailsQuery(bookId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

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

        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Updates an existing book in the global catalog
    /// </summary>
    [HttpPut("{bookId:guid}")]
    [ProducesResponseType(typeof(UpdateBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateBookResponse>> UpdateBook(
        Guid bookId,
        [FromBody] UpdateBookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBookCommand(
            bookId,
            request.Title,
            request.Synopsis,
            request.PageCount,
            request.Publisher,
            request.CoverUrl,
            request.Authors,
            request.Tags
        );

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a book from the global catalog
    /// </summary>
    [HttpDelete("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBookCommand(bookId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

// Request DTOs
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

public record UpdateBookRequest(
    string? Title = null,
    string? Synopsis = null,
    int? PageCount = null,
    string? Publisher = null,
    string? CoverUrl = null,
    List<string>? Authors = null,
    List<string>? Tags = null
);