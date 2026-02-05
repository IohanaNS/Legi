namespace Legi.SharedKernel;

public abstract class BaseAuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
}