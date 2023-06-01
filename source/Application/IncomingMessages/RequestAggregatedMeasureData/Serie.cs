namespace Application.IncomingMessages.RequestAggregatedMeasureData;

public record Serie(
    string Id,
    string SettlementSeriesVersion,
    string MarketEvaluationPointType,
    string MarketEvaluationSettlementMethod,
    string StartDateAndOrTimeDateTime,
    string EndDateAndOrTimeDateTime,
    string MeteringGridAreaDomainId,
    string BiddingZoneDomainId,
    string EnergySupplierMarketParticipantId,
    string BalanceResponsiblePartyMarketParticipantId) : IMarketActivityRecord;
