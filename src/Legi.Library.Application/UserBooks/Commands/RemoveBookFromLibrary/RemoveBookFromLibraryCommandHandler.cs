using Legi.Library.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;

public class RemoveBookFromLibraryCommandHandler : IRequestHandler<RemoveBookFromLibraryCommand, Unit>
{
    private readonly IUserBookRepository _userBookRepository;
    private readonly IUserListRepository _userListRepository;
    
    public RemoveBookFromLibraryCommandHandler(IUserBookRepository userBookRepository, IUserListRepository userListRepository)
    {
        _userBookRepository = userBookRepository;
        _userListRepository = userListRepository;
    }
    
    public async Task<Unit> Handle(RemoveBookFromLibraryCommand request, CancellationToken cancellationToken)
    {
        var userBook = await _userBookRepository.GetByIdAsync(request.UserId, request.BookId, cancellationToken);
        if (userBook == null)
            throw new NotFoundException($"Book with ID '{request.BookId}' not found for user '{request.UserId}'.");

        if(userBook.UserId != request.UserId)
            throw new UnauthorizedAccessException("You do not have permission to remove this book from your library");
        
        var listsWithBook = await _userListRepository.GetListsContainingBookAsync(
            userBook.Id, cancellationToken);
 
        foreach (var list in listsWithBook)
        {
            list.RemoveBookIfExists(userBook.Id);
            await _userListRepository.UpdateAsync(list, cancellationToken);
        }
 
        await _userBookRepository.UpdateAsync(userBook, cancellationToken);
 
        return Unit.Value;
    }
}