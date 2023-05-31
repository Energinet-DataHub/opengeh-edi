namespace Application.IncomingMessages.RequestAggregatedMeasureData;

public record Serie(
    string Id) : IMarketActivityRecord;
