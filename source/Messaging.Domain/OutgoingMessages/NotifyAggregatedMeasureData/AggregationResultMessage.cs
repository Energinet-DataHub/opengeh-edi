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

using System.Text.Json;
using Messaging.Domain.Actors;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;

namespace Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

public class AggregationResultMessage : OutgoingMessage
{
    private AggregationResultMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, JsonSerializer.Serialize(messageRecord))
    {
        Series = JsonSerializer.Deserialize<TimeSeries>(messageRecord)!;
    }

    private AggregationResultMessage(ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, TimeSeries series)
        : base(MessageType.NotifyAggregatedMeasureData, receiverId, transactionId, processType, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, JsonSerializer.Serialize(series))
    {
        Series = series;
    }

    public TimeSeries Series { get; }

    public static AggregationResultMessage Create(ActorNumber receiverNumber, MarketRole receiverRole, TransactionId transactionId, ProcessType processType, Series result)
    {
        ArgumentNullException.ThrowIfNull(transactionId);
        ArgumentNullException.ThrowIfNull(processType);
        ArgumentNullException.ThrowIfNull(result);

        var series = new TimeSeries(
            transactionId.Id,
            result.GridAreaCode,
            result.MeteringPointType,
            result.MeasureUnitType,
            result.Resolution,
            result.Points.Select(p => new Point(p.Position, p.Quantity, p.Quality, p.SampleTime)).ToList());

        return new AggregationResultMessage(
            receiverNumber,
            transactionId,
            processType.Code,
            receiverRole,
            series);
    }
}

public record TimeSeries(string TransactionId, string GridAreaCode, string MeteringPointType, string MeasureUnitType, string Resolution, IReadOnlyList<Point> Point);

public record Point(int Position, decimal? Quantity, string? Quality, string SampleTime);
