using FluentValidation;
using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Isbn)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ISBN is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("ISBN is required");

        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Title is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title must be at most 500 characters");

        RuleFor(x => x.Synopsis)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Synopsis is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Synopsis is required");

        RuleFor(x => x.Publisher)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Publisher is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Publisher is required");

        RuleFor(x => x.CoverUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Cover URL is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Cover URL is required")
            .Must(HaveValidHttpUrl)
            .WithMessage("Cover URL must be a valid HTTP or HTTPS URL");

        RuleFor(x => x.PageCount)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Page count is required")
            .GreaterThan(0)
            .WithMessage("Page count must be greater than zero");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required");

        RuleFor(x => x.Authors)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Authors are required")
            .Must(a => a is { Count: > 0 })
            .WithMessage("At least one author is required")
            .Must(a => a is not null && a.Count <= Book.MaxAuthors)
            .WithMessage($"Book cannot have more than {Book.MaxAuthors} authors");

        RuleForEach(x => x.Authors)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Author name is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Author name is required");

        RuleFor(x => x.Tags)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Tags are required")
            .Must(tags => tags is { Count: > 0 })
            .WithMessage("At least one tag is required")
            .Must(tags => tags is not null && tags.Count <= Book.MaxTags)
            .WithMessage($"Book cannot have more than {Book.MaxTags} tags");

        RuleForEach(x => x.Tags)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Tag name is required")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Tag name is required");
    }

    private static bool HaveValidHttpUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
