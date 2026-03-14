using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveUserBookRating;

public class RemoveUserBookRatingCommandHandler
    : IRequestHandler<RemoveUserBookRatingCommand, Unit>
{
    private readonly IUserBookRepository _userBookRepository;

    public RemoveUserBookRatingCommandHandler(IUserBookRepository userBookRepository)
    {
        _userBookRepository = userBookRepository;
    }

    public async Task<Unit> Handle(
        RemoveUserBookRatingCommand request,
        CancellationToken cancellationToken)
    {
        var userBook = await _userBookRepository.GetByIdAsync(
                           request.UserBookId, cancellationToken)
                       ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        userBook.RemoveRating();

        await _userBookRepository.UpdateAsync(userBook, cancellationToken);

        return Unit.Value;
    }
}