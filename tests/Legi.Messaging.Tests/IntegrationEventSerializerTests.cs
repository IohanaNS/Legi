using Legi.Contracts;
using Legi.Messaging.Serialization;

namespace Legi.Messaging.Tests;

public class IntegrationEventSerializerTests
{
    public enum Flavour { Vanilla, Chocolate }

    // A probe event with an enum field. Contracts use strings at the boundary
    // today, so this exists only to verify the serializer's enum policy (6E.4).
    public sealed record EnumProbeIntegrationEvent(Flavour Flavour, int Count) : IIntegrationEvent;

    private readonly IntegrationEventSerializer _serializer = new();

    [Fact]
    public void Serialize_EncodesEnumAsName_NotOrdinalInt()
    {
        var (_, payload) = _serializer.Serialize(new EnumProbeIntegrationEvent(Flavour.Chocolate, 3));

        Assert.Contains("\"flavour\":\"Chocolate\"", payload);
        Assert.DoesNotContain("\"flavour\":1", payload);
    }

    [Fact]
    public void RoundTrip_PreservesEnumValue()
    {
        var (typeName, payload) = _serializer.Serialize(
            new EnumProbeIntegrationEvent(Flavour.Chocolate, 3));

        var back = Assert.IsType<EnumProbeIntegrationEvent>(_serializer.Deserialize(typeName, payload));

        Assert.Equal(Flavour.Chocolate, back.Flavour);
        Assert.Equal(3, back.Count);
    }

    [Fact]
    public void Deserialize_AlsoAcceptsStringEnum_FromOlderOrHandwrittenPayloads()
    {
        var typeName = typeof(EnumProbeIntegrationEvent).AssemblyQualifiedName!;
        var back = (EnumProbeIntegrationEvent)_serializer.Deserialize(
            typeName, "{\"flavour\":\"Vanilla\",\"count\":7}");

        Assert.Equal(Flavour.Vanilla, back.Flavour);
        Assert.Equal(7, back.Count);
    }
}
