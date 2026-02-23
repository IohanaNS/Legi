using Legi.Catalog.Application.Books.Commands.UpdateBook;
using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Application.Tests.Books.Commands.UpdateBook;

public class UpdateBookCommandValidatorTests
{
    private readonly UpdateBookCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenBookIdIsEmpty()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.Empty, Title: "Some Title");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book ID is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoFieldsProvided()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "At least one field must be provided for update");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid(), Title: string.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title cannot be empty");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTitleExceedsMaxLength()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid(), Title: new string('A', 501));

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title must be at most 500 characters");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageCountIsZero()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid(), PageCount: 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page count must be greater than zero");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorsAreEmpty()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid(), Authors: []);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "At least one author is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorsExceedLimit()
    {
        // Arrange
        var authors = Enumerable.Range(1, Book.MaxAuthors + 1)
            .Select(i => $"Author {i}")
            .ToList();

        var command = new UpdateBookCommand(Guid.NewGuid(), Authors: authors);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book cannot have more than 10 authors");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsExceedLimit()
    {
        // Arrange
        var tags = Enumerable.Range(1, Book.MaxTags + 1)
            .Select(i => $"tag-{i}")
            .ToList();

        var command = new UpdateBookCommand(Guid.NewGuid(), Tags: tags);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book cannot have more than 30 tags");
    }

    [Fact]
    public void Validate_ShouldPass_WhenOnlyTitleProvided()
    {
        // Arrange
        var command = new UpdateBookCommand(Guid.NewGuid(), Title: "Valid Title");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenOnlyTagsProvided()
    {
        // Arrange - empty tags list is valid (clears tags)
        var command = new UpdateBookCommand(Guid.NewGuid(), Tags: []);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
