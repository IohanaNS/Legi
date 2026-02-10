using FluentValidation;

namespace Legi.Catalog.Application.Books.Commands.UpdateBook;

public class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("Book ID is required");

        // At least one field must be provided for update
        RuleFor(x => x)
            .Must(cmd => 
                cmd.Title != null || 
                cmd.Synopsis != null || 
                cmd.PageCount != null || 
                cmd.Publisher != null || 
                cmd.CoverUrl != null || 
                cmd.Authors != null || 
                cmd.Tags != null)
            .WithMessage("At least one field must be provided for update");

        When(x => x.Title != null, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty")
                .MaximumLength(500).WithMessage("Title must be at most 500 characters");
        });

        When(x => x.PageCount.HasValue, () =>
        {
            RuleFor(x => x.PageCount!.Value)
                .GreaterThan(0).WithMessage("Page count must be greater than zero");
        });

        When(x => x.Authors != null, () =>
        {
            RuleFor(x => x.Authors!)
                .Must(authors => authors.Count > 0)
                .WithMessage("At least one author is required")
                .Must(authors => authors.Count <= 10)
                .WithMessage("Book cannot have more than 10 authors");
        });

        When(x => x.Tags != null, () =>
        {
            RuleFor(x => x.Tags!.Count)
                .LessThanOrEqualTo(30)
                .WithMessage("Book cannot have more than 30 tags");
        });
    }
}