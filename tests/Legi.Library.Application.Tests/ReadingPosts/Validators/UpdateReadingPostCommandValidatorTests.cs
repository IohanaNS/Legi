using Legi.Library.Application.ReadingPosts.Commands.UpdateReadingPost;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;

namespace Legi.Library.Application.Tests.ReadingPosts.Validators;

public class UpdateReadingPostCommandValidatorTests
{
    private readonly UpdateReadingPostCommandValidator _validator = new();

    [Fact]
    public void Validate_PostIdIsEmpty_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithPostId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Post ID is required.");
    }

    [Fact]
    public void Validate_UserIdIsEmpty_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithUserId(Guid.Empty)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "User ID is required.");
    }

    [Fact]
    public void Validate_ContentAndProgressMissing_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithContent(" ")
            .WithProgress(null, null)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Post must have content or progress (or both).");
    }

    [Fact]
    public void Validate_ContentTooLong_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithContent(new string('a', ReadingProgress.MaxContentLength + 1))
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == $"Content must be at most {ReadingProgress.MaxContentLength} characters.");
    }

    [Fact]
    public void Validate_ProgressValueWithoutType_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithProgress(50, null)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress type is required when progress value is provided.");
    }

    [Fact]
    public void Validate_ProgressTypeWithoutValue_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithProgress(null, ProgressType.Page)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress value is required when progress type is provided.");
    }

    [Fact]
    public void Validate_NegativeProgressValue_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithProgress(-1, ProgressType.Page)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Progress value cannot be negative.");
    }

    [Fact]
    public void Validate_PercentageProgressAboveMaximum_Fails()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithProgress(101, ProgressType.Percentage)
            .Build();

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Percentage progress cannot exceed 100.");
    }

    [Fact]
    public void Validate_ValidContentOnlyCommand_Passes()
    {
        var command = UpdateReadingPostCommandBuilder.Valid()
            .WithContent("A useful note.")
            .WithProgress(null, null)
            .Build();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
