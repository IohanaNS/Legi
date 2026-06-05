using System.Text.RegularExpressions;
using FluentValidation;

namespace Legi.Social.Application.Profiles.Queries.SearchUsers;

public class SearchUsersQueryValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersQueryValidator()
    {
        RuleFor(x => x.UsernamePrefix)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Username prefix is required.")
            .Must(x => x.Trim().Length >= 3).WithMessage("Username prefix must be at least 3 characters.")
            .Must(x => x.Trim().Length <= 30).WithMessage("Username prefix must be at most 30 characters.")
            .Must(x => Regex.IsMatch(x.Trim(), @"^[A-Za-z0-9_]+$"))
            .WithMessage("Username prefix must contain only letters, numbers and underscore.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 20)
            .WithMessage("Limit must be between 1 and 20.");
    }
}
