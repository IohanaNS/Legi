namespace Legi.SharedKernel;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}