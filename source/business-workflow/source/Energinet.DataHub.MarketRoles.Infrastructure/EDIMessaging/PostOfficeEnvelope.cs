namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging
{
    public record PostOfficeEnvelope(string Id, string Recipient, string Content, string MessageType);
}
