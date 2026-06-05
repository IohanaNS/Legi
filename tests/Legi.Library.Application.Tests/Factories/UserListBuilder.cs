using Legi.Library.Domain.Entities;

namespace Legi.Library.Application.Tests.Factories;

public sealed class UserListBuilder
{
    private Guid _userId = LibraryTestIds.UserId;
    private string _name = "Favorites";
    private string? _description = "Books worth returning to.";
    private bool _isPublic;
    private readonly List<Guid> _userBookIds = [];

    public static UserListBuilder Valid() => new();

    public UserListBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public UserListBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserListBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public UserListBuilder Public()
    {
        _isPublic = true;
        return this;
    }

    public UserListBuilder WithBook(Guid userBookId)
    {
        _userBookIds.Add(userBookId);
        return this;
    }

    public UserList Build()
    {
        var list = UserList.Create(_userId, _name, _description, _isPublic);

        foreach (var userBookId in _userBookIds)
        {
            list.AddBook(userBookId);
        }

        return list;
    }
}
