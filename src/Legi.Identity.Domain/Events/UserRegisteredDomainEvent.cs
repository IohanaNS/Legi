using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserRegisteredDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public DateTime OccurredOn { get; }

    public UserRegisteredDomainEvent(Guid userId, string email)
    {
        UserId = userId;
        Email = email;
        OccurredOn = DateTime.UtcNow;
    }
}
