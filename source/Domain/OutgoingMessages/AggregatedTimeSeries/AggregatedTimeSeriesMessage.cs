using Domain.Actors;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions;
using Energinet.DataHub.Edi.Responses.AggregatedMeasureData;
using Point = Domain.Transactions.Aggregations.Point;

namespace Domain.OutgoingMessages.AggregatedTimeSeries;

public class AggregatedTimeSeriesMessage : OutgoingMessage
{
    public AggregatedTimeSeriesMessage()
        : base()
    {

    }

    public object Create(
        ActorNumber receiverNumber,
        MarketRole receiverRole,
        ProcessId processId,
        AggregatedTimeSeriesRequestAccepted result)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(receiverNumber);

        var series = result.Series
            .Select(serie => new AggregatedTimeSeries(serie.Version,s))


        return new AggregationResultMessage(
            receiverNumber,
            processId,
            EnumerationType.FromName<BusinessReason>(result.BusinessReason).Name,
            receiverRole,
            series);
    }

    public record AggregatedTimeSeries(
        string MessageId,
        string Version,
        string SettlementVersion);


    /*
     "mRID": "string",
            "version": "string ?",
            "settlement_Series.version": "ProcessVariantKind_String ?",
            "originalTransactionIDReference_Series.mRID": "string ?",
            "marketEvaluationPoint.type": "marketEvaluationPointKind_String ",
            "marketEvaluationPoint.settlementMethod": "SettlementMethodKind_string ?",
            "assert_MktPSRType.psrType": "PsrType_String ?",
            "registration_DateAndOrTime-DateTime": "DateTime ?",
            "biddingZone_Domain.mRID": "AreaID_String ?",
            "meteringGridArea_Domain.mRID": "AreaID:_String ?",
            "in_Domain.mRID": "AreaID_String ?      (Bruges ikke i datahub 3.0 )",
            "out_Domain.mRID": "AreaID_String ?      (Bruges ikke i datahub 3.0 )",
            "energySullplier_MarketParticipant-mRID": "PartyID_String ?",
            "balanceResponsibleParty_MarketParticipant-mRID": "PartyID_String ?",
            "product": "String ?",
            "quantity_Measure_Unit.name": "MeasurementUnitKind_String",
            "period": {
                "resolution": "PT1H",
                "timeInterval": {
                    "start": {
                        "value": "2022-02-12T23:00Z"
                    },
                    "end": {
                        "value": "2022-02-13T23:00Z"
                    }
                }
            },
            "point": [{
                "position": "Position_Integer",
                "quantity": "Decimal ?",
                "quality": "Quality_String ?"
            }

     *
     * */
    */

}
