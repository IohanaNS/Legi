using System.Text.Json;
using Legi.Contracts;

namespace Legi.Messaging.Serialization;

/// <summary>
/// Centralizes serialization of integration events for the outbox and
/// deserialization on the consumer side. Uses System.Text.Json with default
/// options.
/// 
/// Type discrimination is by assembly-qualified name, persisted in the
/// outbox row's Type column. This makes type renames or assembly moves
/// breaking changes for any messages already in flight — acceptable for
/// Legi's single-monorepo deployment model.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, section 7.5.
/// </summary>
public class IntegrationEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes an integration event and returns its payload and the
    /// assembly-qualified type name to record alongside it.
    /// </summary>
    public (string TypeName, string Payload) Serialize<T>(T @event)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(@event);

        var typeName = typeof(T).AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Type '{typeof(T).FullName}' has no assembly-qualified name. " +
                "This is unexpected for an integration event.");

        var payload = JsonSerializer.Serialize(@event, typeof(T), Options);
        return (typeName, payload);
    }

    /// <summary>
    /// Deserializes a payload using the recorded type name. Throws if the
    /// type cannot be resolved (likely a rename/move since the message was
    /// produced).
    /// </summary>
    public IIntegrationEvent Deserialize(string typeName, string payload)
    {
        var type = Type.GetType(typeName, throwOnError: false)
            ?? throw new InvalidOperationException(
                $"Cannot resolve integration event type '{typeName}'. " +
                "The type may have been renamed, moved, or removed since the " +
                "message was produced.");

        var deserialized = JsonSerializer.Deserialize(payload, type, Options)
            ?? throw new InvalidOperationException(
                $"Deserialization of type '{typeName}' returned null.");

        if (deserialized is not IIntegrationEvent integrationEvent)
            throw new InvalidOperationException(
                $"Deserialized type '{typeName}' is not an IIntegrationEvent. " +
                "This indicates corrupt outbox data or a type registry conflict.");

        return integrationEvent;
    }
}