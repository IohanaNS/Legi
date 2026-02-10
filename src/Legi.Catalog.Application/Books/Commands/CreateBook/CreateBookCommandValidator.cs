using FluentValidation;
using Legi.Catalog.Domain.Entities;

namespace Legi.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty()
            .WithMessage("ISBN is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(500)
            .WithMessage("Title must be at most 500 characters");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required");

        RuleFor(x => x.Authors)
            .NotNull()
            .WithMessage("Authors are required")
            .Must(a => a is { Count: > 0 })
            .WithMessage("At least one author is required")
            .Must(a => a is not null && a.Count <= Book.MaxAuthors)
            .WithMessage($"Book cannot have more than {Book.MaxAuthors} authors");

        RuleForEach(x => x.Authors)
            .NotEmpty()
            .WithMessage("Author name is required");

        When(x => x.PageCount.HasValue, () =>
        {
            RuleFor(x => x.PageCount!.Value)
                .GreaterThan(0)
                .WithMessage("Page count must be greater than zero");
        });

        When(x => x.Tags is not null, () =>
        {
            RuleFor(x => x.Tags!)
                .Must(tags => tags.Count <= Book.MaxTags)
                .WithMessage($"Book cannot have more than {Book.MaxTags} tags");

            RuleForEach(x => x.Tags!)
                .NotEmpty()
                .WithMessage("Tag name is required");
        });
    }
}
