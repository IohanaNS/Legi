using Legi.Library.Application.UserLists.Commands.AddBookToList;
using Legi.Library.Application.UserLists.Commands.CreateUserList;
using Legi.Library.Application.UserLists.Commands.DeleteUserList;
using Legi.Library.Application.UserLists.Commands.RemoveBookFromList;
using Legi.Library.Application.UserLists.Commands.UpdateUserList;
using Legi.Library.Application.UserLists.Queries.GetListBooks;
using Legi.Library.Application.UserLists.Queries.GetListDetails;
using Legi.Library.Application.UserLists.Queries.GetMyLists;
using Legi.Library.Application.UserLists.Queries.SearchPublicLists;
using System.Security.Claims;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Library.Api.Controllers;

[ApiController]
[Route("api/v1/library/lists")]
[Authorize]
public class UserListsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserListsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Get the authenticated user's lists.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyLists(CancellationToken cancellationToken)
    {
        var query = new GetMyListsQuery(GetUserId());
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get list details by ID.
    /// </summary>
    [HttpGet("{listId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetListDetails(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var query = new GetListDetailsQuery(listId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get books in a list.
    /// </summary>
    [HttpGet("{listId:guid}/books")]
    [AllowAnonymous]
    public async Task<IActionResult> GetListBooks(
        Guid listId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetListBooksQuery(listId, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Search public lists.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPublicLists(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchPublicListsQuery(search, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new list.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUserList(
        [FromBody] CreateUserListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserListCommand(
            GetUserId(),
            request.Name,
            request.Description,
            request.IsPublic);

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetListDetails),
            new { listId = result.ListId },
            result);
    }

    /// <summary>
    /// Update a list's name, description, or visibility.
    /// </summary>
    [HttpPatch("{listId:guid}")]
    public async Task<IActionResult> UpdateUserList(
        Guid listId,
        [FromBody] UpdateUserListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserListCommand(
            listId,
            GetUserId(),
            request.Name,
            request.Description,
            request.IsPublic);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a list.
    /// </summary>
    [HttpDelete("{listId:guid}")]
    public async Task<IActionResult> DeleteUserList(
        Guid listId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserListCommand(listId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add a book to a list.
    /// </summary>
    [HttpPost("{listId:guid}/books")]
    public async Task<IActionResult> AddBookToList(
        Guid listId,
        [FromBody] AddBookToListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBookToListCommand(request.UserBookId, listId, GetUserId());
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Remove a book from a list.
    /// </summary>
    [HttpDelete("{listId:guid}/books/{userBookId:guid}")]
    public async Task<IActionResult> RemoveBookFromList(
        Guid listId,
        Guid userBookId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveBookFromListCommand(userBookId, listId, GetUserId());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

// Request DTOs
public record CreateUserListRequest(
    string Name,
    string? Description = null,
    bool IsPublic = false);

public record UpdateUserListRequest(
    string Name,
    string? Description = null,
    bool IsPublic = false);

public record AddBookToListRequest(Guid UserBookId);
