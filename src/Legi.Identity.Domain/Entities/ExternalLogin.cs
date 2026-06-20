using Legi.SharedKernel;

namespace Legi.Identity.Domain.Entities;

public class ExternalLogin : BaseEntity
{
    public string Provider { get; private set; }
    public string ProviderKey { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal ExternalLogin(string provider, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("External login provider is required");

        if (string.IsNullOrWhiteSpace(providerKey))
            throw new DomainException("External login provider key is required");

        Id = Guid.NewGuid();
        Provider = provider;
        ProviderKey = providerKey;
        CreatedAt = DateTime.UtcNow;
    }
}
