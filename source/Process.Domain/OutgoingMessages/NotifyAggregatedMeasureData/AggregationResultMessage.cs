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

using System;
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Process.Domain.Documents;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

public class AggregationResultMessage : OutgoingMessage
{
    private AggregationResultMessage(DocumentType documentType, ActorNumber receiverId, ProcessId processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(documentType, receiverId, processId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    {
        Series = new Serializer().Deserialize<TimeSeries>(messageRecord)!;
    }

    private AggregationResultMessage(ActorNumber receiverId, ProcessId processId, string businessReason, MarketRole receiverRole, TimeSeries series)
        : base(DocumentType.NotifyAggregatedMeasureData, receiverId, processId, businessReason, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, new Serializer().Serialize(series))
    {
        Series = series;
    }

    public TimeSeries Series { get; }

    public static AggregationResultMessage Create(
        ActorNumber receiverNumber,
        MarketRole receiverRole,
        ProcessId processId,
        Aggregation result)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(receiverNumber);

        var series = new TimeSeries(
            processId.Id,
            result.GridAreaDetails.GridAreaCode,
            result.MeteringPointType,
            result.SettlementType,
            result.MeasureUnitType,
            result.Resolution,
            result.ActorGrouping.EnergySupplierNumber,
            result.ActorGrouping.BalanceResponsibleNumber,
            result.Period,
            result.Points.Select(p => new Point(p.Position, p.Quantity, p.Quality, p.SampleTime)).ToList(),
            result.OriginalTransactionIdReference,
            result.SettlementVersion);

        return new AggregationResultMessage(
            receiverNumber,
            processId,
            EnumerationType.FromName<BusinessReason>(result.BusinessReason).Name,
            receiverRole,
            series);
    }
}

public record TimeSeries(
    Guid TransactionId,
    string GridAreaCode,
    string MeteringPointType,
    string? SettlementType,
    string MeasureUnitType,
    string Resolution,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    Period Period,
    IReadOnlyList<Point> Point,
    string? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record Point(int Position, decimal? Quantity, string Quality, string SampleTime);
