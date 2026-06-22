using System.Security.Claims;
using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Books.Commands.DeleteBook;
using Legi.Catalog.Application.Books.Commands.SetBookCover;
using Legi.Catalog.Application.Books.Commands.UpdateBook;
using Legi.Catalog.Application.Books.Queries.GetBookDetails;
using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel.Authorization;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/books")]
public class BooksController : ControllerBase
{
    private const long MaxCoverBytes = 5 * 1024 * 1024; // 5 MB
    private const long MaxCoverRequestBytes = MaxCoverBytes + 256 * 1024; // + multipart overhead
    private static readonly HashSet<string> AllowedCoverContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IMediator _mediator;
    private readonly IBookCoverImageProcessor _coverImageProcessor;
    private readonly IBookCoverStorage _coverStorage;

    public BooksController(
        IMediator mediator,
        IBookCoverImageProcessor coverImageProcessor,
        IBookCoverStorage coverStorage)
    {
        _mediator = mediator;
        _coverImageProcessor = coverImageProcessor;
        _coverStorage = coverStorage;
    }

    /// <summary>
    /// Search books in the global catalog with filters and pagination
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(SearchBooksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchBooksResponse>> SearchBooks(
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? authorSlug = null,
        [FromQuery] string[]? tagSlugs = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BookSortBy sortBy = BookSortBy.Relevance,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchBooksQuery(
            GetAuthenticatedUserId(),
            searchTerm,
            authorSlug,
            tagSlugs,
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
    /// Any authenticated user can create a book. Only admins can update, delete, or upload covers.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(CreateBookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateBookResponse>> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

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
    [Authorize(Policy = LegiAuthPolicies.CanManageCatalogBooks)]
    [HttpPut("{bookId:guid}")]
    [ProducesResponseType(typeof(UpdateBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [Authorize(Policy = LegiAuthPolicies.CanManageCatalogBooks)]
    [HttpDelete("{bookId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBook(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBookCommand(bookId);
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Manually upload a cover for a cover-less book — the escape hatch for the
    /// long tail no provider has a cover for. Fill-only: 409 if a cover exists.
    /// </summary>
    [Authorize(Policy = LegiAuthPolicies.CanManageCatalogBooks)]
    [HttpPost("{bookId:guid}/cover")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxCoverRequestBytes)]
    [ProducesResponseType(typeof(SetBookCoverResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadCover(
        Guid bookId,
        [FromForm(Name = "file")] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A non-empty image file is required." });

        if (file.Length > MaxCoverBytes)
            return BadRequest(new { error = $"Image must not exceed {MaxCoverBytes / (1024 * 1024)} MB." });

        if (!AllowedCoverContentTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Only JPG, PNG, or WebP images are supported." });

        await using var stream = file.OpenReadStream();
        var image = await _coverImageProcessor.ProcessAsync(stream, cancellationToken);

        // Store the blob first (keyed by book id), then persist the URL. If the
        // command rejects (book gone / already has a cover), clean up the orphan
        // so a failed upload never leaves a dangling object in the bucket.
        var coverUrl = await _coverStorage.StoreAsync(bookId.ToString("N"), image, cancellationToken);
        try
        {
            var result = await _mediator.Send(
                new SetBookCoverCommand(bookId, GetAuthenticatedUserId(), coverUrl),
                cancellationToken);
            return Ok(result);
        }
        catch
        {
            await _coverStorage.DeleteByUrlAsync(coverUrl, cancellationToken);
            throw;
        }
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid or missing user identity.");

        return userId;
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
