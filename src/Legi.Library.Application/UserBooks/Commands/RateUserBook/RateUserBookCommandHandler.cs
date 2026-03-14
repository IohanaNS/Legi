using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RateUserBook;

public class RateUserBookCommandHandler
    : IRequestHandler<RateUserBookCommand, RateUserBookResponse>
{
    private readonly IUserBookRepository _userBookRepository;

    public RateUserBookCommandHandler(IUserBookRepository userBookRepository)
    {
        _userBookRepository = userBookRepository;
    }

    public async Task<RateUserBookResponse> Handle(
        RateUserBookCommand request,
        CancellationToken cancellationToken)
    {
        var userBook = await _userBookRepository.GetByIdAsync(
                           request.UserBookId, cancellationToken)
                       ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        var rating = Rating.FromStars(request.Stars);
        userBook.Rate(rating);

        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        return new RateUserBookResponse(
            userBook.Id,
            userBook.CurrentRating!.Stars,
            userBook.UpdatedAt);
    }
}
