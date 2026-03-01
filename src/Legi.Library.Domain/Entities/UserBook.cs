using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class UserBook : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid BookId { get; private set; }
    public ReadingStatus Status { get; private set; }
    public Progress? CurrentProgress { get; private set; }
    public bool WishList { get; private set; }
    public Rating? CurrentRating { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    public bool IsDeleted => DeletedAt.HasValue;

    public static UserBook Create(Guid userId, Guid bookId, bool wishList = false)
    {
        var userBook = new UserBook
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BookId = bookId,
            Status = ReadingStatus.NotStarted,
            WishList = wishList,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        
        userBook.AddDomainEvent(
            new BookAddedToLibraryDomainEvent(userId, bookId, wishList));
        return userBook;    
    }

    public void ChangeReadingStatus(ReadingStatus readingStatus)
    {
        if (readingStatus == Status)
            return;
        
        // We should remove the book from the wishlist when the user starts reading the book
        if (readingStatus != ReadingStatus.NotStarted)
            WishList = false;

        if (readingStatus == ReadingStatus.Finished)
            CurrentProgress = Progress.Completed();
        
        #warning Todo: in case the user change the status from finished to something else like reading or not started we have to reset the progress accordinly
        
        UpdatedAt = DateTime.UtcNow;
        var oldStatus = Status;
        Status = readingStatus;
        
        AddDomainEvent(new ReadingStatusChangedDomainEvent(UserId, BookId, oldStatus, Status));
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
            ChangeReadingStatus(ReadingStatus.Finished);
            return; // ChangeStatus already sets UpdatedAt
        }
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rate(Rating rating)
    {
        var oldRating = CurrentRating;
        CurrentRating = rating;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserBookRatedDomainEvent(UserId, BookId, oldRating, CurrentRating));
    }

    public void RemoveRating()
    {
        if(CurrentRating is null)
            return;
        
        var oldRating = CurrentRating;
        CurrentRating = null;
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(
            new UserBookRatingRemovedDomainEvent(UserId, BookId, oldRating));
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