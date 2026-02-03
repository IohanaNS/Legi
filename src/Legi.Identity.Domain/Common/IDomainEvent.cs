namespace Legi.Identity.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}