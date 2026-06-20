using Legi.Identity.Application.Common;
using Legi.Identity.Domain.ValueObjects;

namespace Legi.Identity.Application.Tests.Common;

public class UsernameGeneratorTests
{
    [Theory]
    [InlineData("john.doe@gmail.com", "johndoe")]
    [InlineData("Jane_Doe@example.com", "jane_doe")]
    [InlineData("UPPER@example.com", "upper")]
    [InlineData("a.b.c@x.com", "abc")]
    public void CreateBase_ShouldSanitizeEmailLocalPart(string seed, string expected)
    {
        Assert.Equal(expected, UsernameGenerator.CreateBase(seed));
    }

    [Theory]
    [InlineData("123john", "john")]   // must start with a letter
    [InlineData("___bob", "bob")]
    [InlineData("9to5reader", "to5reader")]
    public void CreateBase_ShouldStripLeadingNonLetters(string seed, string expected)
    {
        Assert.Equal(expected, UsernameGenerator.CreateBase(seed));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]   // no letters
    [InlineData("@@@")]
    [InlineData("ab")]      // shorter than min length
    public void CreateBase_ShouldFallBackWhenSeedIsUnusable(string seed)
    {
        Assert.Equal("reader", UsernameGenerator.CreateBase(seed));
    }

    [Fact]
    public void CreateBase_ShouldClampToMaxLength()
    {
        var seed = new string('a', 50);

        var result = UsernameGenerator.CreateBase(seed);

        Assert.Equal(30, result.Length);
    }

    [Fact]
    public void CreateBase_ShouldAlwaysProduceValidUsername()
    {
        // The output must be accepted by the Username value object.
        var exception = Record.Exception(() => Username.Create(UsernameGenerator.CreateBase("123!!!")));
        Assert.Null(exception);
    }

    [Fact]
    public void WithSuffix_ShouldAppendSuffix()
    {
        Assert.Equal("john42", UsernameGenerator.WithSuffix("john", 42));
    }

    [Fact]
    public void WithSuffix_ShouldKeepResultWithinMaxLength()
    {
        var baseName = new string('a', 30);

        var result = UsernameGenerator.WithSuffix(baseName, 12345);

        Assert.True(result.Length <= 30);
        Assert.EndsWith("12345", result);
        Assert.Null(Record.Exception(() => Username.Create(result)));
    }
}
