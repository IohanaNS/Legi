using Legi.SharedKernel.Mediator;

namespace Legi.Library.Application.UserLists.Commands.CreateUserList;

public class CreateUserListCommand : IRequest<CreateUserListResponse>
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
}