using Newtonsoft.Json;

namespace Messaging.CimMessageAdapter.Messages;

public class MktActivityRecord
{
    [JsonProperty(PropertyName = "mRID")]
    public string Id { get; init; } = string.Empty;

    [JsonProperty(PropertyName = "marketEvaluationPoint.customer_MarketParticipant.mRID")]
    public string? ConsumerId { get; init; }

    [JsonProperty(PropertyName = "marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID")]
    public string? BalanceResponsibleId { get; init; }

    [JsonProperty(PropertyName = "marketEvaluationPoint.energySupplier_MarketParticipant.mRID")]
    public string? EnergySupplierId { get; init; }

    [JsonProperty(PropertyName = "marketEvaluationPoint.mRID")]
    public string MarketEvaluationPointId { get; init; } = string.Empty;

    [JsonProperty(PropertyName = "marketEvaluationPoint.customer_MarketParticipant.name")]
    public string? ConsumerName { get; init; }

    [JsonProperty(PropertyName = "start_DateAndOrTime.dateTime")]
    public string EffectiveDate { get; init; } = string.Empty;
}
