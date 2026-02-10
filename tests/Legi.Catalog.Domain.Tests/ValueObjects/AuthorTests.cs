using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.ValueObjects;

public class AuthorTests
{
    [Fact]
    public void Create_ShouldTrimName_AndGenerateSlug()
    {
        // Act
        var author = Author.Create("  Robert C. Martin  ");

        // Assert
        Assert.Equal("Robert C. Martin", author.Name);
        Assert.Equal("robert-c-martin", author.Slug);
    }

    [Fact]
    public void Create_ShouldNormalizeMultipleSpacesAndSpecialCharactersInSlug()
    {
        // Act
        var author = Author.Create("  Martin   O'Reilly!!  ");

        // Assert
        Assert.Equal("Martin   O'Reilly!!", author.Name);
        Assert.Equal("martin-oreilly", author.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowException_WhenNameIsEmpty(string invalidName)
    {
        // Act
        var act = () => Author.Create(invalidName);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Author name is required", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsTooShort()
    {
        // Act
        var act = () => Author.Create("A");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Author name must be at least 2 characters", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsTooLong()
    {
        // Act
        var act = () => Author.Create(new string('A', 256));

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Author name must be at most 255 characters", exception.Message);
    }

    [Fact]
    public void TwoAuthors_WithEquivalentSlug_ShouldBeEqual()
    {
        // Arrange
        var first = Author.Create("Robert Martin");
        var second = Author.Create("robert   martin");

        // Assert
        Assert.Equal(first, second);
    }
}
