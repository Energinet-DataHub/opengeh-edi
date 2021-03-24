namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public class InternalCommandQuerySettings : IInternalCommandQuerySettings
    {
        public InternalCommandQuerySettings(int batchSize)
        {
            BatchSize = batchSize;
        }

        public int BatchSize { get; }
    }
}
