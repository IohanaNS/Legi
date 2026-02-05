using Legi.SharedKernel;

namespace Legi.Identity.Domain.Events;

public sealed class UserRegisteredDomainEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Name { get; }
    public DateTime OccurredOn { get; }

    public UserRegisteredDomainEvent(Guid userId, string email, string name)
    {
        UserId = userId;
        Email = email;
        Name = name;
        OccurredOn = DateTime.UtcNow;
    }
}