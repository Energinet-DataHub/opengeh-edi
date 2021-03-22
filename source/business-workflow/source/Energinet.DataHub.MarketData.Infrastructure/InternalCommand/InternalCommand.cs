namespace Energinet.DataHub.MarketData.Infrastructure.Outbox
{
    public class InternalCommand
    {
        public int Id { get; set; }

        public string? Command { get; set; }
    }
}
