using Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;
using Legi.Library.Application.UserBooks.Commands.RateUserBook;
using Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;
using Legi.Library.Application.UserBooks.Commands.RemoveUserBookRating;
using Legi.Library.Application.UserBooks.Commands.UpdateUserBook;
using Legi.Library.Application.UserBooks.Queries.GetMyLibrary;
using Legi.Library.Application.UserBooks.Queries.GetMyUserBookByBook;
using Legi.Library.Application.UserBooks.Queries.GetUserLibrary;
using Legi.Library.Domain.Enums;
using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Library.Api.Controllers;

[ApiController]
[Route("api/v1/library")]
[Authorize]
public class UserBooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserBooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Get the authenticated user's library with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyLibrary(
        [FromQuery] ReadingStatus? status,
        [FromQuery] bool? wishlist,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMyLibraryQuery(
            GetUserId(), status, wishlist, search, page, pageSize);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get another user's library (paginated), optionally filtered by status —
    /// e.g. <c>?status=Finished</c> for the books they have read. Drives the
    /// profile "Read" page.
    /// </summary>
    [HttpGet("users/{userId:guid}/books")]
    public async Task<IActionResult> GetUserLibrary(
        Guid userId,
        [FromQuery] ReadingStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserLibraryQuery(userId, GetUserId(), status, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the authenticated user's UserBook for a given book (status, rating,
    /// progress), or 204 if the book is not in their library. Drives the book
    /// details page header.
    /// </summary>
    [HttpGet("by-book/{bookId:guid}")]
    public async Task<IActionResult> GetMyUserBookByBook(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetMyUserBookByBookQuery(GetUserId(), bookId), cancellationToken);

        return result is null ? NoContent() : Ok(result);
    }

    /// <summary>
    /// Add a book to the user's library.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddBookToLibrary(
        [FromBody] AddBookToLibraryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBookToLibraryCommand(
            GetUserId(), request.BookId, request.Wishlist);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetMyLibrary), new { }, result);
    }

    /// <summary>
    /// Update a user book (status, wishlist, progress). Partial update.
    /// </summary>
    [HttpPatch("{userBookId:guid}")]
    public async Task<IActionResult> UpdateUserBook(
        Guid userBookId,
        [FromBody] UpdateUserBookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserBookCommand(
            userBookId,
            GetUserId(),
            request.Status,
            request.Wishlist,
            request.ProgressValue,
            request.ProgressType,
            request.FinishedReadingAt);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove a book from the user's library (soft delete).
    /// </summary>
    [HttpDelete("{userBookId:guid}")]
    public async Task<IActionResult> RemoveBookFromLibrary(
        Guid userBookId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveBookFromLibraryCommand(userBookId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Rate a book (0.5 to 5.0 stars, increments of 0.5).
    /// </summary>
    [HttpPut("{userBookId:guid}/rating")]
    public async Task<IActionResult> RateUserBook(
        Guid userBookId,
        [FromBody] RateUserBookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RateUserBookCommand(userBookId, GetUserId(), request.Stars);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove rating from a book.
    /// </summary>
    [HttpDelete("{userBookId:guid}/rating")]
    public async Task<IActionResult> RemoveUserBookRating(
        Guid userBookId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveUserBookRatingCommand(userBookId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

// Request DTOs (API contracts, separate from commands)
public record AddBookToLibraryRequest(
    Guid BookId,
    bool Wishlist = false);

public record UpdateUserBookRequest(
    ReadingStatus? Status = null,
    bool? Wishlist = null,
    int? ProgressValue = null,
    ProgressType? ProgressType = null,
    DateOnly? FinishedReadingAt = null);

public record RateUserBookRequest(decimal Stars);
