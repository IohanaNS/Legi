namespace Legi.Library.Application.Common.Interfaces;

public interface IUserListVisibilityPolicy
{
    bool CanView(Guid ownerUserId, bool isPublic, Guid viewerUserId);
    bool CanViewPrivateLists(Guid targetUserId, Guid viewerUserId);
}
