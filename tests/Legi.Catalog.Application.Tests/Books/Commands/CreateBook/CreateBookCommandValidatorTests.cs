using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Application.Tests.Books.Commands.CreateBook;

public class CreateBookCommandValidatorTests
{
    private readonly CreateBookCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenIsbnIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(isbn: string.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "ISBN is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorsAreEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(authors: []);

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

        var command = CreateBookCommandFactory.Create(authors: authors);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"Book cannot have more than {Book.MaxAuthors} authors");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorNameIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(authors: ["Robert C. Martin", string.Empty]);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Author name is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageCountIsNotPositive()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(pageCount: 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page count must be greater than zero");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsExceedLimit()
    {
        // Arrange
        var tags = Enumerable.Range(1, Book.MaxTags + 1)
            .Select(i => $"tag-{i}")
            .ToList();

        var command = CreateBookCommandFactory.Create(tags: tags);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"Book cannot have more than {Book.MaxTags} tags");
    }

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
}
