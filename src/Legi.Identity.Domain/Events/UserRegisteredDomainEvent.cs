using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserRegisteredDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Username { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserRegisteredDomainEvent(Guid userId, string username, string email)
    {
        UserId = userId;
        Username = username;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}