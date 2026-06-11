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
    public void Validate_ShouldFail_WhenTitleIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle(string.Empty)
            .Build();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Title is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenAuthorsAreNull()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithoutAuthors()
            .Build();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Authors are required");
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
    public void Validate_ShouldFail_WhenSynopsisIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(synopsis: " ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Synopsis is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPublisherIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(publisher: " ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Publisher is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenPageCountIsMissing()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(pageCount: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page count is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCoverUrlIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(coverUrl: " ");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Cover URL is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCoverUrlIsNotHttpUrl()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(coverUrl: "ftp://example.com/cover.jpg");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Cover URL must be a valid HTTP or HTTPS URL");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsAreNull()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTags(null)
            .Build();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Tags are required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenTagsAreEmpty()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(tags: []);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "At least one tag is required");
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
    public void Validate_ShouldFail_WhenTagNameIsEmpty()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTags(["software-engineering", string.Empty])
            .Build();

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Tag name is required");
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
