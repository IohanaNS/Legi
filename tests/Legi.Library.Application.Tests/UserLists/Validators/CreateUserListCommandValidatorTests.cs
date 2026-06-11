using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Commands.CreateUserList;

namespace Legi.Library.Application.Tests.UserLists.Validators;

public class CreateUserListCommandValidatorTests
{
    private readonly CreateUserListCommandValidator _validator = new();

    [Fact]
    public void Validate_BlankName_Fails()
    {
        var command = CreateUserListCommandBuilder.Valid().WithName("   ").Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "List name cannot be blank.");
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var command = CreateUserListCommandBuilder.Valid().WithName(string.Empty).Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "List name is required.");
    }

    [Fact]
    public void Validate_DuplicateBookIds_Fails()
    {
        var bookId = Guid.NewGuid();
        var command = CreateUserListCommandBuilder.Valid().WithBooks(bookId, bookId).Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "A list cannot contain the same book twice.");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = CreateUserListCommandBuilder.Valid().WithBooks(Guid.NewGuid()).Build();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
