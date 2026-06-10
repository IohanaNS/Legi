using Legi.Library.Application.Common.Interfaces;

namespace Legi.Library.Application.Common.Policies;

public class UserListVisibilityPolicy : IUserListVisibilityPolicy
{
    public bool CanView(Guid ownerUserId, bool isPublic, Guid viewerUserId) =>
        isPublic || ownerUserId == viewerUserId;

    public bool CanViewPrivateLists(Guid targetUserId, Guid viewerUserId) =>
        targetUserId == viewerUserId;
}
