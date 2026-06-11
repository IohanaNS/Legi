using Legi.Library.Application.UserLists.Commands.AddBookToList;
using Legi.Library.Application.UserLists.Commands.CreateUserList;

namespace Legi.Library.Application.Tests.Factories;

public sealed class CreateUserListCommandBuilder
{
    private Guid _userId = LibraryTestIds.UserId;
    private string _name = "Favorites";
    private string? _description = "Books worth returning to.";
    private bool _isPublic;
    private IReadOnlyList<Guid> _bookIds = [];

    public static CreateUserListCommandBuilder Valid() => new();

    public CreateUserListCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public CreateUserListCommandBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateUserListCommandBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public CreateUserListCommandBuilder Public()
    {
        _isPublic = true;
        return this;
    }

    public CreateUserListCommandBuilder WithBooks(params Guid[] bookIds)
    {
        _bookIds = bookIds;
        return this;
    }

    public CreateUserListCommand Build()
    {
        return new CreateUserListCommand(
            _userId,
            _name,
            _description,
            _isPublic,
            _bookIds);
    }
}

public sealed class AddBookToListCommandBuilder
{
    private Guid _bookId = LibraryTestIds.BookId;
    private Guid _listId = LibraryTestIds.UserListId;
    private Guid _userId = LibraryTestIds.UserId;

    public static AddBookToListCommandBuilder Valid() => new();

    public AddBookToListCommandBuilder WithBookId(Guid bookId)
    {
        _bookId = bookId;
        return this;
    }

    public AddBookToListCommandBuilder WithListId(Guid listId)
    {
        _listId = listId;
        return this;
    }

    public AddBookToListCommandBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public AddBookToListCommand Build() => new(_bookId, _listId, _userId);
}
