using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.UpdateUserBook;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.Tests.UserBooks.Validators;

public class UpdateUserBookCommandValidatorTests
{
    private readonly UpdateUserBookCommandValidator _validator = new();

    [Fact]
    public void Validate_UserBookIdIsEmpty_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserBookId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "UserBook ID is required.");
    }

    [Fact]
    public void Validate_UserIdIsEmpty_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithUserId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User ID is required.");
    }

    [Fact]
    public void Validate_ProgressValueWithoutType_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithProgress(50, null)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress type is required when progress value is provided.");
    }

    [Fact]
    public void Validate_ProgressTypeWithoutValue_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithProgress(null, ProgressType.Page)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress value is required when progress type is provided.");
    }

    [Fact]
    public void Validate_NegativeProgressValue_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithProgress(-1, ProgressType.Page)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress value cannot be negative.");
    }

    [Fact]
    public void Validate_PercentageProgressAboveMaximum_Fails()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithProgress(101, ProgressType.Percentage)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Percentage progress cannot exceed 100.");
    }

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = UpdateUserBookCommandBuilder.Valid()
            .WithStatus(ReadingStatus.Reading)
            .WithProgress(42, ProgressType.Page)
            .Build();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
