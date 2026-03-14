using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.AddBookToList;

public class AddBookToListCommandHandler
    : IRequestHandler<AddBookToListCommand, AddBookToListResponse>
{
    private readonly IUserListRepository _userListRepository;
    private readonly IUserBookRepository _userBookRepository;

    public AddBookToListCommandHandler(
        IUserListRepository userListRepository,
        IUserBookRepository userBookRepository)
    {
        _userListRepository = userListRepository;
        _userBookRepository = userBookRepository;
    }

    public async Task<AddBookToListResponse> Handle(
        AddBookToListCommand request,
        CancellationToken cancellationToken)
    {
        var list = await _userListRepository.GetByIdAsync(
                       request.ListId, cancellationToken)
                   ?? throw new NotFoundException("UserList", request.ListId);

        if (list.UserId != request.UserId)
            throw new ForbiddenException();

        var userBook = await _userBookRepository.GetByIdAsync(
                           request.UserBookId, cancellationToken)
                       ?? throw new NotFoundException("UserBook", request.UserBookId);

        if (userBook.UserId != request.UserId)
            throw new ForbiddenException();

        list.AddBook(request.UserBookId);

        await _userListRepository.UpdateAsync(list, cancellationToken);

        return new AddBookToListResponse(
            list.Id,
            request.UserBookId,
            list.BooksCount);
    }
}