using CimMessageAdapter.Messages.Queues;

namespace CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class RequestAggregatedMeasureDataReceiver : MessageReceiver<RequestAggregatedMeasureDataTransaction>
{
    public RequestAggregatedMeasureDataReceiver(
        IMessageIds messageIds,
        IMessageQueueDispatcher<RequestAggregatedMeasureDataTransaction> messageQueueDispatcher,
        ITransactionIds transactionIds,
        SenderAuthorizer senderAuthorizer,
        ProcessTypeValidator processTypeValidator,
        MessageTypeValidator messageTypeValidator)
        : base(messageIds, messageQueueDispatcher, transactionIds, senderAuthorizer, processTypeValidator, messageTypeValidator)
    {
    }
}
