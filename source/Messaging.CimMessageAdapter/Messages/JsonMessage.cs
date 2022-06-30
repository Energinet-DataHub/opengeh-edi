using System.Collections.ObjectModel;
using Messaging.Application.IncomingMessages;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Newtonsoft.Json;

namespace Messaging.CimMessageAdapter.Messages;

public class JsonMessage
{
    [JsonProperty(PropertyName = "mRID")]
    public string? MessageId { get; set; }

    [JsonProperty(PropertyName = "process.processType")]
    public string? ProcessType { get; set; }

    [JsonProperty(PropertyName = "sender_MarketParticipant.mRID")]
    public string? SenderId { get; set; }

    [JsonProperty(PropertyName = "sender_MarketParticipant.marketRole.type")]
    public string? SenderRole { get; set; }

    [JsonProperty(PropertyName = "receiver_MarketParticipant.mRID")]
    public string? ReceiverId { get; set; }

    [JsonProperty(PropertyName = "receiver_MarketParticipant.marketRole.type")]
    public string? ReceiverRole { get; set; }

    [JsonProperty(PropertyName = "createdDateTime")]
    public string? CreatedAt { get; set; }

    #pragma warning disable
    [JsonProperty(PropertyName = "MktActivityRecord")]
    public Collection<MktActivityRecord>? MarketActivityRecords { get; set; }
    #pragma warning restore
}
