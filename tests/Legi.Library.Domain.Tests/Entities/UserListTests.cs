using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.Tests.Factories;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.Entities;

public class UserListTests
{
    [Fact]
    public void Create_ValidData_CreatesListWithDefaultCounters()
    {
        var list = UserListBuilder.Valid()
            .Public()
            .Build();

        Assert.NotEqual(Guid.Empty, list.Id);
        Assert.Equal(LibraryTestIds.UserId, list.UserId);
        Assert.Equal("Favorites", list.Name);
        Assert.True(list.IsPublic);
        Assert.Equal(0, list.BooksCount);
        Assert.Equal(0, list.LikesCount);
        Assert.Equal(0, list.CommentsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_InvalidName_ThrowsDomainException(string name)
    {
        Assert.Throws<DomainException>(() =>
            UserListBuilder.Valid().WithName(name).Build());
    }

    [Fact]
    public void Create_NameTooLong_ThrowsDomainException()
    {
        var name = new string('a', UserList.MaxNameLength + 1);

        Assert.Throws<DomainException>(() =>
            UserListBuilder.Valid().WithName(name).Build());
    }

    [Fact]
    public void Create_DescriptionTooLong_ThrowsDomainException()
    {
        var description = new string('a', UserList.MaxDescriptionLength + 1);

        Assert.Throws<DomainException>(() =>
            UserListBuilder.Valid().WithDescription(description).Build());
    }

    [Fact]
    public void UpdateDetails_ValidData_TrimsAndUpdatesFields()
    {
        var list = UserListBuilder.Valid().Build();

        list.UpdateDetails(" Updated ", " New description ", isPublic: true);

        Assert.Equal("Updated", list.Name);
        Assert.Equal("New description", list.Description);
        Assert.True(list.IsPublic);
    }

    [Fact]
    public void AddBook_NewBook_AddsItemAndIncrementsBooksCount()
    {
        var list = UserListBuilder.Valid().Build();

        var item = list.AddBook(LibraryTestIds.UserBookId);

        Assert.Equal(LibraryTestIds.UserBookId, item.UserBookId);
        Assert.Equal(0, item.Order);
        Assert.Equal(1, list.BooksCount);
        Assert.Single(list.Items);
    }

    [Fact]
    public void AddBook_DuplicateBook_ThrowsDomainException()
    {
        var list = UserListBuilder.Valid()
            .WithBook(LibraryTestIds.UserBookId)
            .Build();

        Assert.Throws<DomainException>(() => list.AddBook(LibraryTestIds.UserBookId));
    }

    [Fact]
    public void RemoveBook_ExistingBook_RemovesItemAndDecrementsBooksCount()
    {
        var list = UserListBuilder.Valid()
            .WithBook(LibraryTestIds.UserBookId)
            .Build();

        list.RemoveBook(LibraryTestIds.UserBookId);

        Assert.Empty(list.Items);
        Assert.Equal(0, list.BooksCount);
    }

    [Fact]
    public void RemoveBook_MissingBook_ThrowsDomainException()
    {
        var list = UserListBuilder.Valid().Build();

        Assert.Throws<DomainException>(() => list.RemoveBook(LibraryTestIds.UserBookId));
    }

    [Fact]
    public void RemoveBookIfExists_MissingBook_DoesNotThrowOrMutateCount()
    {
        var list = UserListBuilder.Valid().Build();

        list.RemoveBookIfExists(LibraryTestIds.UserBookId);

        Assert.Empty(list.Items);
        Assert.Equal(0, list.BooksCount);
    }

    [Fact]
    public void ReorderBooks_ContainsAllBooks_AssignsRequestedOrder()
    {
        var first = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var second = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var list = UserListBuilder.Valid()
            .WithBook(first)
            .WithBook(second)
            .Build();

        list.ReorderBooks([second, first]);

        Assert.Equal(1, list.Items.Single(x => x.UserBookId == first).Order);
        Assert.Equal(0, list.Items.Single(x => x.UserBookId == second).Order);
    }

    [Fact]
    public void ReorderBooks_MissingBook_ThrowsDomainException()
    {
        var list = UserListBuilder.Valid()
            .WithBook(LibraryTestIds.UserBookId)
            .Build();

        Assert.Throws<DomainException>(() =>
            list.ReorderBooks([LibraryTestIds.OtherBookId]));
    }

    [Fact]
    public void SocialCounters_DecrementAtZero_StayAtZero()
    {
        var list = UserListBuilder.Valid().Build();

        list.DecrementLikes();
        list.DecrementComments();

        Assert.Equal(0, list.LikesCount);
        Assert.Equal(0, list.CommentsCount);
    }

    [Fact]
    public void Delete_Always_RaisesUserListDeletedEvent()
    {
        var list = UserListBuilder.Valid().Build();

        list.Delete();

        var domainEvent = Assert.IsType<UserListDeletedDomainEvent>(
            Assert.Single(list.DomainEvents));
        Assert.Equal(list.Id, domainEvent.UserListId);
        Assert.Equal(list.UserId, domainEvent.UserId);
    }
}
