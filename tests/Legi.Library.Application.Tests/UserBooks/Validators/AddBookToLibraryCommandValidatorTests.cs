using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;

namespace Legi.Library.Application.Tests.UserBooks.Validators;

public class AddBookToLibraryCommandValidatorTests
{
    private readonly AddBookToLibraryCommandValidator _validator = new();

    [Fact]
    public void Validate_UserIdIsEmpty_Fails()
    {
        var command = AddBookToLibraryCommandBuilder.Valid()
            .WithUserId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User ID is required.");
    }

    [Fact]
    public void Validate_BookIdIsEmpty_Fails()
    {
        var command = AddBookToLibraryCommandBuilder.Valid()
            .WithBookId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book ID is required.");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = AddBookToLibraryCommandBuilder.Valid().Build();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
