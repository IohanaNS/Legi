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
        return new UserList
        {
            UserId = userId,
            Name = name,
            Description = description,
            IsPublic = isPublic,
            LikesCount = 0,
            CommentsCount = 0,
            BooksCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateDetails(string name, string? description, bool isPublic)
    {
        ValidateName(name);
        ValidateDescription(description);

        Name = name.Trim();
        Description = description?.Trim();
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
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

    public UserListItem AddBook(Guid userBookId)
    {
        if (_items.Any(x => x.UserBookId == userBookId))
            throw new DomainException("The book is already in this list");

        var item = UserListItem.Create(userBookId, _items.Count);
        _items.Add(item);
        
        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
        
        return item;
    }

    public void RemoveBook(Guid userBookId)
    {
        var item = _items.FirstOrDefault(i => i.UserBookId == userBookId);
        if (item is null)
            throw new DomainException("Book is not in this list");
        
        _items.Remove(item);
        _items.Order();
        
        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes all items referencing the given UserBookId.
    /// Used when a UserBook is soft-deleted.
    /// Does not throw if the book is not in the list.
    /// </summary>
    public void RemoveBookIfExists(Guid userBookId)
    {
        var item = _items.FirstOrDefault(i => i.UserBookId == userBookId);
        if (item is null)
            return;
        
        _items.Remove(item);
        _items.Order();
        
        BooksCount = _items.Count;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ReorderBooks(IReadOnlyList<Guid> userBookIdsInOrder)
    {
        if (userBookIdsInOrder.Count != _items.Count)
            throw new DomainException(
                "Reorder list must contain all books in the list");

        for (var i = 0; i < userBookIdsInOrder.Count; i++)
        {
            var item = _items.FirstOrDefault(x => x.UserBookId == userBookIdsInOrder[i]);
            if (item is null)
                throw new DomainException(
                    $"Book {userBookIdsInOrder[i]} is not in this list");

            item.Order = i;
        }

        UpdatedAt = DateTime.UtcNow;
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