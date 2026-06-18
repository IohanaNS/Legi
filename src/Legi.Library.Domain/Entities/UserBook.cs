using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class UserBook : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }

    /// <summary>
    /// The Catalog work this book (edition) belongs to. Identity for work-level
    /// aggregation/grouping. Sourced from the book's <c>BookSnapshot.WorkId</c> at
    /// add time. See Docs/CATALOG-FEATURE-editions.md.
    /// </summary>
    public Guid WorkId { get; private set; }

    public ReadingStatus Status { get; private set; }
    public Progress? CurrentProgress { get; private set; }
    public bool WishList { get; private set; }
    public Rating? CurrentRating { get; private set; }

    /// <summary>
    /// When the user finished reading this cycle. Nullable even while
    /// <see cref="ReadingStatus.Finished"/> — null means "finished, date unknown"
    /// and is excluded from date-bucketed statistics (kept out of monthly/yearly
    /// counts) while still counting toward "total books read".
    /// </summary>
    public DateOnly? FinishedReadingAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    
    public bool IsDeleted => DeletedAt.HasValue;

    public static UserBook Create(Guid userId, Guid bookId, Guid workId, bool wishList = false)
    {
        if (workId == Guid.Empty)
            throw new DomainException("WorkId is required");

        var userBook = new UserBook
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BookId = bookId,
            WorkId = workId,
            Status = ReadingStatus.NotStarted,
            WishList = wishList,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        
        userBook.AddDomainEvent(
            new BookAddedToLibraryDomainEvent(userBook.Id, userId, bookId, workId, wishList));
        return userBook;
    }

    /// <summary>
    /// Changes the reading status. When transitioning to <see cref="ReadingStatus.Finished"/>,
    /// <paramref name="finishedOn"/> records when the book was finished — it may be null,
    /// meaning "finished, date unknown". The finish date is cleared when reverting away from
    /// Finished. To edit the date of an already-finished book, use
    /// <see cref="SetFinishedReadingDate"/>.
    /// </summary>
    public void ChangeReadingStatus(ReadingStatus readingStatus, DateOnly? finishedOn = null)
    {
        if (readingStatus == Status)
            return;

        // We should remove the book from the wishlist when the user starts reading the book
        if (readingStatus != ReadingStatus.NotStarted)
            WishList = false;

        if (readingStatus == ReadingStatus.Finished)
        {
            ValidateFinishDate(finishedOn);
            CurrentProgress = Progress.Completed();
            FinishedReadingAt = finishedOn;
        }

        // Reset progress and finish date when reverting from Finished to another status
        if (Status == ReadingStatus.Finished && readingStatus != ReadingStatus.Finished)
        {
            CurrentProgress = null;
            FinishedReadingAt = null;
        }

        UpdatedAt = DateTime.UtcNow;
        var oldStatus = Status;
        Status = readingStatus;
        
        AddDomainEvent(new ReadingStatusChangedDomainEvent(UserId, BookId, WorkId, oldStatus, Status));
    }

    public void SetWishList(bool wishList)
    {
        if(wishList && Status != ReadingStatus.NotStarted)
            throw new DomainException("Cannot be added to wish list if the reading has already started");   
        
        WishList = wishList;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the current reading progress.
    /// Page upper bound validation (against PageCount) is done externally.
    /// Auto-transitions to Finished when percentage reaches 100%.
    /// For Page type, completion detection is handled by the command handler
    /// (which has access to BookSnapshot.PageCount).
    /// </summary>
    public void UpdateProgress(Progress progress)
    {
        CurrentProgress = progress;
        
        if (progress.Type == ProgressType.Percentage
            && progress.Value == Progress.MaxPercentage
            && Status != ReadingStatus.Finished)
        {
            // Reaching 100% means the book is finished right now.
            ChangeReadingStatus(ReadingStatus.Finished, DateOnly.FromDateTime(DateTime.UtcNow));
            return; // ChangeStatus already sets UpdatedAt
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rate(Rating rating, bool isPartOfReview = false)
    {
        var oldRating = CurrentRating;
        CurrentRating = rating;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserBookRatedDomainEvent(UserId, BookId, WorkId, oldRating, CurrentRating, isPartOfReview));
    }

    public void RemoveRating()
    {
        if(CurrentRating is null)
            return;
        
        var oldRating = CurrentRating;
        CurrentRating = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(
            new UserBookRatingRemovedDomainEvent(UserId, BookId, WorkId, oldRating));
    }

    /// <summary>
    /// Edits the finish date of an already-finished book. Pass null to reset it to
    /// "date unknown". Throws if the book is not currently Finished, or the date is
    /// in the future.
    /// </summary>
    public void SetFinishedReadingDate(DateOnly? date)
    {
        if (Status != ReadingStatus.Finished)
            throw new DomainException("Cannot set a finish date when the book is not finished");

        ValidateFinishDate(date);
        FinishedReadingAt = date;
        UpdatedAt = DateTime.UtcNow;
    }

    // Tolerate one day of client/server timezone skew (the client sends its local
    // "today"); reject anything clearly in the future.
    private static void ValidateFinishDate(DateOnly? date)
    {
        if (date.HasValue && date.Value > DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1))
            throw new DomainException("Finish date cannot be in the future");
    }

    /// <summary>
    /// Soft deletes this UserBook. ReadingPosts are preserved (history).
    /// UserListItems should be hard-deleted by a domain event handler.
    /// </summary>
    public void Remove()
    {
        if (IsDeleted)
            throw new DomainException("Book is already removed from library");

        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(
            new BookRemovedFromLibraryDomainEvent(Id, UserId, BookId));
    }
}