using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Queries.GetMyUserBookByBook;

public class GetMyUserBookByBookQueryHandler
    : IRequestHandler<GetMyUserBookByBookQuery, UserBookDto?>
{
    private readonly IUserBookReadRepository _readRepository;

    public GetMyUserBookByBookQueryHandler(IUserBookReadRepository readRepository)
    {
        _readRepository = readRepository;
    }

    public Task<UserBookDto?> Handle(
        GetMyUserBookByBookQuery request,
        CancellationToken cancellationToken)
    {
        return _readRepository.GetByUserAndBookAsync(
            request.UserId,
            request.BookId,
            cancellationToken);
    }
}
