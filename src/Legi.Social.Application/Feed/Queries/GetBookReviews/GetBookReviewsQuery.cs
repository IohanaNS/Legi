using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Feed.Queries.GetBookReviews;

/// <summary>
/// Gets the reviews written for a book (for the book details page).
/// ViewerUserId is optional — anonymous visitors see IsLikedByMe = false.
/// </summary>
public record GetBookReviewsQuery(
    Guid BookId,
    Guid? ViewerUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<FeedItemDto>>;
