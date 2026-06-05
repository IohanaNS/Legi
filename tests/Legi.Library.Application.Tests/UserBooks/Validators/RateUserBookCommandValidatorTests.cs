using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.RateUserBook;

namespace Legi.Library.Application.Tests.UserBooks.Validators;

public class RateUserBookCommandValidatorTests
{
    private readonly RateUserBookCommandValidator _validator = new();

    [Fact]
    public void Validate_UserBookIdIsEmpty_Fails()
    {
        var command = RateUserBookCommandBuilder.Valid()
            .WithUserBookId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserBook ID is required.");
    }

    [Fact]
    public void Validate_UserIdIsEmpty_Fails()
    {
        var command = RateUserBookCommandBuilder.Valid()
            .WithUserId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User ID is required.");
    }

    [Theory]
    [InlineData("0.0")]
    [InlineData("5.5")]
    public void Validate_RatingOutsideAllowedRange_Fails(string starsValue)
    {
        var command = RateUserBookCommandBuilder.Valid()
            .WithStars(decimal.Parse(starsValue, System.Globalization.CultureInfo.InvariantCulture))
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Rating must be between 0.5 and 5.0 stars.");
    }

    [Fact]
    public void Validate_RatingNotHalfStarIncrement_Fails()
    {
        var command = RateUserBookCommandBuilder.Valid()
            .WithStars(4.25m)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Rating must be in increments of 0.5 stars.");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = RateUserBookCommandBuilder.Valid()
            .WithStars(4.5m)
            .Build();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
