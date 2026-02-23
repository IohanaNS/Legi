using Legi.Catalog.Application.Books.Commands.DeleteBook;

namespace Legi.Catalog.Application.Tests.Books.Commands.DeleteBook;

public class DeleteBookCommandValidatorTests
{
    private readonly DeleteBookCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenBookIdIsEmpty()
    {
        // Arrange
        var command = new DeleteBookCommand(Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book ID is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenBookIdIsValid()
    {
        // Arrange
        var command = new DeleteBookCommand(Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
