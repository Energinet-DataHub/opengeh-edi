// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Domain.Actors;
using Domain.Documents;
using Domain.Transactions;

namespace Domain.OutgoingMessages.AggregatedTimeSeries;

public class AggregatedTimeSeriesMessage : OutgoingMessage
{
    public AggregatedTimeSeriesMessage(DocumentType documentType, ActorNumber receiverId, TransactionId transactionId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(documentType, receiverId, transactionId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    {
    }

    public AggregatedTimeSeriesMessage()
    {

    }
    //https://energinet.dk/media/0nsklumj/20220422-edi-transaktioner-for-danske-elmarkede-cim-ver-1_4.pdf#page=81&zoom=100,72,114
    //Page 67
    //Same of the strings underneath has types. Which may be described in the link above, not sure
    //This is work in progress!!!! AJW
    public string Mrid;
    public string? Version;
    public string? SettlementSeriesVersion;
    public string? OriginalTransactionReferenc;
    public string? MarketEvaluationType;
    public string? MarketEvalationSettlementMethod;
    public string? MktpsType;
    public string? RegistrationTime;
    public string? BiddingDomain;
    public string? MetingsGridArea;
    public string? EnergiSupplier;
    public string? BallanceResponsible;
    public string? Product; // this is a enum in the protobuf contract
    public string  QuntityMeasureUnit // type in protobuf
    public Period Period;


    private record Period(string resolution, TimeInterval timeinterval);

    private record TimeInterval(string start, string end)

    /*public object Create(
        ActorNumber receiverNumber,
        MarketRole receiverRole,
        ProcessId processId,
        AggregatedTimeSeriesRequestAccepted result)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(receiverNumber);

        // delete
        return new AggregatedTimeSeries("1", "2", "3");

        var series = result.Series
            .Select(serie => new AggregatedTimeSeries(serie.Version,s))


        return new AggregationResultMessage(
            receiverNumber,
            processId,
            EnumerationType.FromName<BusinessReason>(result.BusinessReason).Name,
            receiverRole,
            series);
    }*/
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
}

/*
public record AggregatedTimeSeries(
    string MessageId,
    string Version,
    string SettlementVersion);
*/
