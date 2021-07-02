namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI
{
    public record PostOfficeEnvelope(string Id, string Recipient, string Content, string MessageType);
}
