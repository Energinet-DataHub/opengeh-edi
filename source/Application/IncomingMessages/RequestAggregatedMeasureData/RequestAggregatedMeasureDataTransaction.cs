using Application.Configuration.Commands.Commands;
using MediatR;

namespace Application.IncomingMessages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataTransaction : ICommand<Unit>, IMarketTransaction<Series>
{
    public RequestAggregatedMeasureDataTransaction(MessageHeader message, Series marketActivityRecord)
    {
        Message = message;
        MarketActivityRecord = marketActivityRecord;
    }

    public MessageHeader Message { get; }

    public Series MarketActivityRecord { get; }
}
