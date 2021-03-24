namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    /// <summary>
    /// Settings for the Internal Command query
    /// </summary>
    public interface IInternalCommandQuerySettings
    {
        /// <summary>
        /// Defines how many elements get fetched from the DB on each loop
        /// </summary>
        int BatchSize { get; }
    }
}
