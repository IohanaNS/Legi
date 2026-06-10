using Legi.Library.Application.Common.Policies;
using Legi.Library.Application.Tests.Factories;

namespace Legi.Library.Application.Tests.Common.Policies;

public class UserListVisibilityPolicyTests
{
    private readonly UserListVisibilityPolicy _policy = new();

    [Fact]
    public void CanView_PublicList_ReturnsTrue()
    {
        var result = _policy.CanView(
            LibraryTestIds.UserId,
            isPublic: true,
            LibraryTestIds.OtherUserId);

        Assert.True(result);
    }

    [Fact]
    public void CanView_PrivateListOwnedByViewer_ReturnsTrue()
    {
        var result = _policy.CanView(
            LibraryTestIds.UserId,
            isPublic: false,
            LibraryTestIds.UserId);

        Assert.True(result);
    }

    [Fact]
    public void CanView_PrivateListOwnedByAnotherUser_ReturnsFalse()
    {
        var result = _policy.CanView(
            LibraryTestIds.UserId,
            isPublic: false,
            LibraryTestIds.OtherUserId);

        Assert.False(result);
    }

    [Fact]
    public void CanViewPrivateLists_ViewerIsTarget_ReturnsTrue()
    {
        var result = _policy.CanViewPrivateLists(
            LibraryTestIds.UserId,
            LibraryTestIds.UserId);

        Assert.True(result);
    }

    [Fact]
    public void CanViewPrivateLists_ViewerIsNotTarget_ReturnsFalse()
    {
        var result = _policy.CanViewPrivateLists(
            LibraryTestIds.UserId,
            LibraryTestIds.OtherUserId);

        Assert.False(result);
    }
}
