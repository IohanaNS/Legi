using Legi.Library.Application.UserLists.Commands.UpdateUserList;
using Legi.Library.Domain.Entities;

namespace Legi.Library.Application.Tests.UserLists.Validators;

public class UpdateUserListCommandValidatorTests
{
    private readonly UpdateUserListCommandValidator _validator = new();

    [Fact]
    public void Validate_ListIdIsEmpty_Fails()
    {
        var command = CreateCommand(listId: Guid.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "List ID is required.");
    }

    [Fact]
    public void Validate_UserIdIsEmpty_Fails()
    {
        var command = CreateCommand(userId: Guid.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User ID is required.");
    }

    [Fact]
    public void Validate_NameIsEmpty_Fails()
    {
        var command = CreateCommand(name: string.Empty);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "List name is required.");
    }

    [Fact]
    public void Validate_NameTooShort_Fails()
    {
        var command = CreateCommand(name: "A");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"List name must be at least {UserList.MinNameLength} characters.");
    }

    [Fact]
    public void Validate_NameTooLong_Fails()
    {
        var command = CreateCommand(name: new string('a', UserList.MaxNameLength + 1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"List name must be at most {UserList.MaxNameLength} characters.");
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var command = CreateCommand(description: new string('a', UserList.MaxDescriptionLength + 1));

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"Description must be at most {UserList.MaxDescriptionLength} characters.");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = CreateCommand();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    private static UpdateUserListCommand CreateCommand(
        Guid? listId = null,
        Guid? userId = null,
        string name = "Favorites",
        string? description = "Books worth returning to.",
        bool isPublic = true)
    {
        return new UpdateUserListCommand(
            listId ?? Guid.Parse("44444444-4444-4444-4444-444444444444"),
            userId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            name,
            description,
            isPublic);
    }
}
