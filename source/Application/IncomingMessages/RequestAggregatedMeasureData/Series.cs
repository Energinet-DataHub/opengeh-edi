namespace Application.IncomingMessages.RequestAggregatedMeasureData;

public record Series(
    string Id) : IMarketActivityRecord;
