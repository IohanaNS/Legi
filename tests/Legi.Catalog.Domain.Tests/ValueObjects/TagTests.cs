using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Tests.ValueObjects;

public class TagTests
{
    [Fact]
    public void Create_ShouldTrimName_AndGenerateSlug()
    {
        // Act
        var tag = Tag.Create("  Software Engineering  ");

        // Assert
        Assert.Equal("Software Engineering", tag.Name);
        Assert.Equal("software-engineering", tag.Slug);
    }

    [Fact]
    public void Create_ShouldNormalizeSpecialCharactersInSlug()
    {
        // Act
        var tag = Tag.Create(" C# / .NET ");

        // Assert
        Assert.Equal("C# / .NET", tag.Name);
        Assert.Equal("c-net", tag.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrowException_WhenNameIsEmpty(string invalidName)
    {
        // Act
        var act = () => Tag.Create(invalidName);

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Tag name is required", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsTooShort()
    {
        // Act
        var act = () => Tag.Create("A");

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Tag name must be at least 2 characters", exception.Message);
    }

    [Fact]
    public void Create_ShouldThrowException_WhenNameIsTooLong()
    {
        // Act
        var act = () => Tag.Create(new string('T', 51));

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Tag name must be at most 50 characters", exception.Message);
    }

    [Fact]
    public void TwoTags_WithEquivalentSlug_ShouldBeEqual()
    {
        // Arrange
        var first = Tag.Create("Clean Code");
        var second = Tag.Create("clean   code");

        // Assert
        Assert.Equal(first, second);
    }
}
