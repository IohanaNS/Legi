using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Events;
using Legi.Catalog.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.Entities;

public class BookTests
{
    [Fact]
    public void Create_ShouldTrimFieldsAndInitializeDefaults()
    {
        // Arrange
        var createdByUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var book = Book.Create(
            IsbnFactory.Create(),
            "  Clean Code  ",
            [AuthorFactory.Create()],
            createdByUserId,
            "  Great book  ",
            464,
            "  Prentice Hall  ",
            "  https://example.com/cover.jpg  ",
            [TagFactory.Create()]
        );

        // Assert
        Assert.Equal("Clean Code", book.Title);
        Assert.Equal("Great book", book.Synopsis);
        Assert.Equal("Prentice Hall", book.Publisher);
        Assert.Equal("https://example.com/cover.jpg", book.CoverUrl);
        Assert.Equal(createdByUserId, book.CreatedByUserId);
        Assert.Equal(0, book.AverageRating);
        Assert.Equal(0, book.RatingsCount);
        Assert.Equal(0, book.ReviewsCount);
        Assert.Single(book.Authors);
        Assert.Single(book.Tags);
    }

    [Fact]
    public void Create_ShouldRaiseBookCreatedEvent_WithExpectedPayload()
    {
        // Arrange
        var createdByUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // Act
        var book = Book.Create(
            IsbnFactory.Create(),
            "Clean Architecture",
            [AuthorFactory.Create("Robert C. Martin")],
            createdByUserId
        );

        // Assert
        Assert.Single(book.DomainEvents);

        var domainEvent = Assert.IsType<BookCreatedDomainEvent>(book.DomainEvents.First());
        Assert.Equal(book.Id, domainEvent.BookId);
        Assert.Equal(book.Isbn.Value, domainEvent.Isbn);
        Assert.Equal("Clean Architecture", domainEvent.Title);
        Assert.Equal(createdByUserId, domainEvent.CreatedByUserId);
        Assert.Single(domainEvent.Authors);
        Assert.Equal("Robert C. Martin", domainEvent.Authors[0]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowException_WhenTitleIsEmpty(string invalidTitle)
    {
        // Act
        var act = () => BookFactory.Create(title: invalidTitle);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Title is required", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenTitleIsTooLong()
    {
        // Act
        var act = () => BookFactory.Create(title: new string('A', 501));

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Title must be at most 500 characters", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenAuthorsAreEmpty()
    {
        // Act
        var act = () => BookFactory.Create(authors: []);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("At least one author is required", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenAuthorsExceedLimit()
    {
        // Arrange
        var tooManyAuthors = AuthorFactory.CreateMany(Book.MaxAuthors + 1);

        // Act
        var act = () => BookFactory.Create(authors: tooManyAuthors);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal($"Book cannot have more than {Book.MaxAuthors} authors", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowException_WhenPageCountIsNotPositive(int invalidPageCount)
    {
        // Act
        var act = () => BookFactory.Create(pageCount: invalidPageCount);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Page count must be greater than zero", exception.Message);
    }

    [Fact]
    public void AddAuthor_ShouldIgnoreDuplicateAuthor()
    {
        // Arrange
        var existingAuthor = AuthorFactory.Create("Robert C. Martin");
        var book = BookFactory.Create(authors: [existingAuthor]);

        // Act
        book.AddAuthor(AuthorFactory.Create("Robert C Martin"));

        // Assert
        Assert.Single(book.Authors);
    }

    [Fact]
    public void AddAuthor_ShouldIgnoreDuplicateAuthor_EvenWhenAtMaxLimit()
    {
        // Arrange
        var authors = AuthorFactory.CreateMany(Book.MaxAuthors);
        var book = BookFactory.Create(authors: authors);

        // Act
        var exception = Record.Exception(() => book.AddAuthor(authors[0]));

        // Assert
        Assert.Null(exception);
        Assert.Equal(Book.MaxAuthors, book.Authors.Count);
    }

    [Fact]
    public void AddAuthor_ShouldThrowException_WhenAuthorLimitIsExceeded()
    {
        // Arrange
        var book = BookFactory.Create(authors: AuthorFactory.CreateMany(Book.MaxAuthors));

        // Act
        var act = () => book.AddAuthor(AuthorFactory.Create("New Author"));

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal($"Book cannot have more than {Book.MaxAuthors} authors", exception.Message);
    }

    [Fact]
    public void RemoveAuthor_ShouldRemoveAuthor_WhenMoreThanOneAuthorExists()
    {
        // Arrange
        var first = AuthorFactory.Create("Author One");
        var second = AuthorFactory.Create("Author Two");
        var book = BookFactory.Create(authors: [first, second]);

        // Act
        book.RemoveAuthor(first);

        // Assert
        Assert.Single(book.Authors);
        Assert.Equal("author-two", book.Authors.First().Slug);
    }

    [Fact]
    public void RemoveAuthor_ShouldThrowException_WhenTryingToRemoveLastAuthor()
    {
        // Arrange
        var book = BookFactory.Create(authors: [AuthorFactory.Create()]);

        // Act
        var act = () => book.RemoveAuthor(AuthorFactory.Create());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Book must have at least one author", exception.Message);
    }

    [Fact]
    public void SetAuthors_ShouldReplaceAuthorCollection()
    {
        // Arrange
        var book = BookFactory.Create(authors: [AuthorFactory.Create("Original Author")]);
        var newAuthors = new[] { AuthorFactory.Create("Author One"), AuthorFactory.Create("Author Two") };

        // Act
        book.SetAuthors(newAuthors);

        // Assert
        Assert.Equal(2, book.Authors.Count);
        Assert.Contains(book.Authors, a => a.Slug == "author-one");
        Assert.Contains(book.Authors, a => a.Slug == "author-two");
    }

    [Fact]
    public void SetAuthors_ShouldThrowException_WhenListIsEmpty()
    {
        // Arrange
        var book = BookFactory.Create();

        // Act
        var act = () => book.SetAuthors([]);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("At least one author is required", exception.Message);
    }

    [Fact]
    public void AddTag_ShouldIgnoreDuplicateTag_EvenWhenAtMaxLimit()
    {
        // Arrange
        var tags = TagFactory.CreateMany(Book.MaxTags);
        var book = BookFactory.Create(tags: tags);

        // Act
        var exception = Record.Exception(() => book.AddTag(tags[0]));

        // Assert
        Assert.Null(exception);
        Assert.Equal(Book.MaxTags, book.Tags.Count);
    }

    [Fact]
    public void AddTag_ShouldThrowException_WhenTagLimitIsExceeded()
    {
        // Arrange
        var book = BookFactory.Create(tags: TagFactory.CreateMany(Book.MaxTags));

        // Act
        var act = () => book.AddTag(TagFactory.Create("new-tag"));

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal($"Book cannot have more than {Book.MaxTags} tags", exception.Message);
    }

    [Fact]
    public void AddTags_ShouldRaiseTagsUpdatedEvent_AndIgnoreDuplicates()
    {
        // Arrange
        var book = BookFactory.Create(tags: [TagFactory.Create("architecture")]);
        book.ClearDomainEvents();

        // Act
        book.AddTags([TagFactory.Create("architecture"), TagFactory.Create("clean-code")]);

        // Assert
        Assert.Equal(2, book.Tags.Count);
        Assert.Contains(book.Tags, t => t.Slug == "architecture");
        Assert.Contains(book.Tags, t => t.Slug == "clean-code");

        var updatedEvent = Assert.IsType<BookTagsUpdatedDomainEvent>(book.DomainEvents.Single());
        Assert.Contains("architecture", updatedEvent.Tags);
        Assert.Contains("clean-code", updatedEvent.Tags);
    }

    [Fact]
    public void RemoveTag_ShouldRaiseTagsUpdatedEvent_WhenTagExists()
    {
        // Arrange
        var existingTag = TagFactory.Create("architecture");
        var book = BookFactory.Create(tags: [existingTag, TagFactory.Create("clean-code")]);
        book.ClearDomainEvents();

        // Act
        book.RemoveTag(existingTag);

        // Assert
        Assert.Single(book.Tags);
        Assert.DoesNotContain(book.Tags, t => t.Slug == "architecture");
        Assert.IsType<BookTagsUpdatedDomainEvent>(book.DomainEvents.Single());
    }

    [Fact]
    public void ClearTags_ShouldRemoveAllTags_AndRaiseTagsUpdatedEvent()
    {
        // Arrange
        var book = BookFactory.Create(tags: [TagFactory.Create("architecture"), TagFactory.Create("clean-code")]);
        book.ClearDomainEvents();

        // Act
        book.ClearTags();

        // Assert
        Assert.Empty(book.Tags);
        var updatedEvent = Assert.IsType<BookTagsUpdatedDomainEvent>(book.DomainEvents.Single());
        Assert.Empty(updatedEvent.Tags);
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var book = BookFactory.Create(
            title: "Original Title",
            synopsis: "Original synopsis",
            pageCount: 100,
            publisher: "Original Publisher",
            coverUrl: "https://example.com/original.jpg"
        );

        // Act
        book.UpdateDetails(title: "Updated Title", pageCount: 200);

        // Assert
        Assert.Equal("Updated Title", book.Title);
        Assert.Equal("Original synopsis", book.Synopsis);
        Assert.Equal(200, book.PageCount);
        Assert.Equal("Original Publisher", book.Publisher);
        Assert.Equal("https://example.com/original.jpg", book.CoverUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_ShouldThrowException_WhenTitleIsEmpty(string invalidTitle)
    {
        // Arrange
        var book = BookFactory.Create();

        // Act
        var act = () => book.UpdateDetails(title: invalidTitle);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Title cannot be empty", exception.Message);
    }

    [Fact]
    public void RecalculateRating_ShouldRoundAndRaiseDomainEvent()
    {
        // Arrange
        var book = BookFactory.Create();
        book.ClearDomainEvents();

        // Act
        book.RecalculateRating(4.236m, 120);

        // Assert
        Assert.Equal(4.24m, book.AverageRating);
        Assert.Equal(120, book.RatingsCount);

        var domainEvent = Assert.IsType<BookRatingRecalculatedDomainEvent>(book.DomainEvents.Single());
        Assert.Equal(book.Id, domainEvent.BookId);
        Assert.Equal(4.24m, domainEvent.NewAverageRating);
        Assert.Equal(120, domainEvent.TotalRatings);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void RecalculateRating_ShouldThrowException_WhenAverageIsOutOfRange(decimal invalidAverage)
    {
        // Arrange
        var book = BookFactory.Create();

        // Act
        var act = () => book.RecalculateRating(invalidAverage, 10);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Average rating must be between 0 and 5", exception.Message);
    }

    [Fact]
    public void UpdateReviewsCount_ShouldThrowException_WhenCountIsNegative()
    {
        // Arrange
        var book = BookFactory.Create();

        // Act
        var act = () => book.UpdateReviewsCount(-1);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Reviews count cannot be negative", exception.Message);
    }
}
