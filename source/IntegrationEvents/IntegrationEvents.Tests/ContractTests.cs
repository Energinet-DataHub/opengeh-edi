using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Google.Protobuf;

namespace IntegrationEvents.Tests;

public class ContractTests
{
    [Theory(DisplayName = nameof(Must_define_an_identifier))]
    [MemberData(nameof(GetAllIntegrationEvents))]
    public void Must_define_an_identifier(Type integrationEvent)
    {
        var hasIdField = integrationEvent?.GetField("IdFieldNumber") is not null;
        Assert.True(hasIdField);
    }

    private static IEnumerable<object[]> GetAllIntegrationEvents()
    {
        return typeof(ConsumerMovedIn)
            .Assembly
            .GetTypes()
            .Where(type => type.GetInterfaces().Contains(typeof(IMessage)))
            .Select(integrationEvent => new[] { integrationEvent });
    }
}
