using Application.Configuration.Commands.Commands;
using MediatR;

namespace Application.IncomingMessages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataTransaction : ICommand<Unit>, IMarketTransaction<Serie>
{
    public RequestAggregatedMeasureDataTransaction(MessageHeader message, Serie marketActivityRecord)
    {
        Message = message;
        MarketActivityRecord = marketActivityRecord;
    }

    public MessageHeader Message { get; }

    public Serie MarketActivityRecord { get; }
}
