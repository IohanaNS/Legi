using Legi.Catalog.Domain.Tests.Factories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.ValueObjects;

public class IsbnTests
{
    [Theory]
    [InlineData("978-0-13-235088-4")]
    [InlineData("978 0 13 235088 4")]
    [InlineData("9780132350884")]
    public void Create_ShouldNormalizeValidIsbn13(string rawIsbn)
    {
        // Act
        var isbn = Isbn.Create(rawIsbn);

        // Assert
        Assert.Equal(IsbnFactory.DefaultIsbn13, isbn.Value);
    }

    [Fact]
    public void Create_ShouldAcceptValidIsbn10()
    {
        // Act
        var isbn = IsbnFactory.CreateIsbn10();

        // Assert
        Assert.Equal(IsbnFactory.DefaultIsbn10, isbn.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowException_WhenIsbnIsEmpty(string value)
    {
        // Act
        var act = () => Isbn.Create(value);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("ISBN is required", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenLengthIsInvalid()
    {
        // Act
        var act = () => Isbn.Create("12345");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("ISBN must be 10 or 13 characters", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenIsbn13ChecksumIsInvalid()
    {
        // Act
        var act = () => Isbn.Create("9780132350885");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Invalid ISBN-13 checksum", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenIsbn10ChecksumIsInvalid()
    {
        // Act
        var act = () => Isbn.Create("0132350881");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Invalid ISBN-10 checksum", exception.Message);
    }

    [Fact]
    public void TwoIsbns_WithSameNormalizedValue_ShouldBeEqual()
    {
        // Arrange
        var first = Isbn.Create("978-0-13-235088-4");
        var second = Isbn.Create("9780132350884");

        // Assert
        Assert.Equal(first, second);
    }
}
