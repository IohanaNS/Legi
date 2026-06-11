using Legi.Library.Domain.Events;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class UserList : BaseAuditableEntity
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 50;
    public const int MaxDescriptionLength = 500;

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public int LikesCount { get; private set; }
    public int CommentsCount { get; private set; }
    public int BooksCount { get; private set; }
    private readonly List<UserListItem> _items = [];
    public IReadOnlyCollection<UserListItem> Items => _items.AsReadOnly();

    public static UserList Create(Guid userId, string name, string? description, bool isPublic = false)
    {
        ValidateName(name);
        ValidateDescription(description);
        var list = new UserList
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Description = description?.Trim(),
            IsPublic = isPublic,
            LikesCount = 0,
            CommentsCount = 0,
            BooksCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        list.AddDomainEvent(new UserListCreatedDomainEvent(list.Id, list.UserId, list.Name, list.IsPublic));
        return list;
    }

    public void UpdateDetails(string name, string? description, bool isPublic)
    {
        ValidateName(name);
        ValidateDescription(description);

        Name = name.Trim();
        Description = description?.Trim();
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserListUpdatedDomainEvent(Id, UserId, Name, IsPublic));
    }

    private static void ValidateDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return;
        if (description.Trim().Length > MaxDescriptionLength)
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters.");
    }

    private static void ValidateName(string name)
    {
        if(string.IsNullOrEmpty(name))
            throw new DomainException("Name cannot be empty.");

        if(name.Length is < MinNameLength or > MaxNameLength)
            throw new DomainException($"Name should be at least {MinNameLength} and at most {MaxNameLength} characters long.");
    }
    
    #region BookManagement

    public UserListItem AddBook(Guid bookId)
    {
        if (_items.Any(x => x.BookId == bookId))
            throw new DomainException("The book is already in this list");

        var item = UserListItem.Create(bookId, _items.Count);
        _items.Add(item);

        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;

        return item;
    }

    public void RemoveBook(Guid bookId)
    {
        var item = _items.FirstOrDefault(i => i.BookId == bookId);
        if (item is null)
            throw new DomainException("Book is not in this list");

        _items.Remove(item);
        Reindex();

        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes the item referencing the given book if present.
    /// Does not throw if the book is not in the list.
    /// </summary>
    public void RemoveBookIfExists(Guid bookId)
    {
        var item = _items.FirstOrDefault(i => i.BookId == bookId);
        if (item is null)
            return;

        _items.Remove(item);
        Reindex();

        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReorderBooks(IReadOnlyList<Guid> bookIdsInOrder)
    {
        if (bookIdsInOrder.Count != _items.Count)
            throw new DomainException(
                "Reorder list must contain all books in the list");

        for (var i = 0; i < bookIdsInOrder.Count; i++)
        {
            var item = _items.FirstOrDefault(x => x.BookId == bookIdsInOrder[i]);
            if (item is null)
                throw new DomainException(
                    $"Book {bookIdsInOrder[i]} is not in this list");

            item.Order = i;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reconciles the list's books to exactly match <paramref name="bookIdsInOrder"/>:
    /// removes items no longer present, adds new ones, and assigns <c>Order</c> by
    /// position. Existing items keep their original <c>AddedAt</c>. Used by the
    /// create/edit flow which submits the full desired set of books.
    /// </summary>
    public void SyncBooks(IReadOnlyList<Guid> bookIdsInOrder)
    {
        if (bookIdsInOrder.Distinct().Count() != bookIdsInOrder.Count)
            throw new DomainException("A list cannot contain the same book twice");

        var desired = bookIdsInOrder;

        _items.RemoveAll(i => !desired.Contains(i.BookId));

        for (var i = 0; i < desired.Count; i++)
        {
            var existing = _items.FirstOrDefault(x => x.BookId == desired[i]);
            if (existing is null)
                _items.Add(UserListItem.Create(desired[i], i));
            else
                existing.Order = i;
        }

        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Reindex()
    {
        var ordered = _items.OrderBy(i => i.Order).ToList();
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].Order = i;
    }

    #endregion
    #region Social Counters (updated via integration events from Social)

    public void IncrementLikes() => LikesCount++;

    public void DecrementLikes()
    {
        if (LikesCount > 0) LikesCount--;
    }

    public void IncrementComments() => CommentsCount++;

    public void DecrementComments()
    {
        if (CommentsCount > 0) CommentsCount--;
    }

    #endregion
    
    public void Delete()
    {
        AddDomainEvent(new UserListDeletedDomainEvent(Id, UserId));
    }
}