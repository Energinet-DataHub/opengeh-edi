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

namespace Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

public class AggregatedTimeSeriesMessage : OutgoingMessage
{
    private AggregatedTimeSeriesMessage(MessageType messageType, ActorNumber receiverId, string transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, JsonSerializer.Serialize(messageRecord))
    {
        TimeSeries = JsonSerializer.Deserialize<TimeSeries>(messageRecord)!;
    }

    private AggregatedTimeSeriesMessage(ActorNumber receiverId, string transactionId, string processType, MarketRole receiverRole, TimeSeries timeSeries)
        : base(MessageType.NotifyAggregatedMeasureData, receiverId, transactionId, processType, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, JsonSerializer.Serialize(timeSeries))
    {
        TimeSeries = timeSeries;
    }

    public TimeSeries TimeSeries { get; }

    public static AggregatedTimeSeriesMessage Create(TimeSeries timeSeries, ActorNumber receiverNumber, MarketRole receiverRole, string transactionId, ProcessType processType)
    {
        ArgumentNullException.ThrowIfNull(processType);

        return new AggregatedTimeSeriesMessage(
            receiverNumber,
            transactionId,
            processType.Code,
            receiverRole,
            timeSeries);
    }
}

public record TimeSeries(Guid Id, string GridAreaCode, string MeteringPointType, string MeasureUnitType, Period Period);

public record Period(string Resolution, TimeInterval TimeInterval, IReadOnlyList<Point> Point);

public record TimeInterval(string Start, string End);

public record Point(int Position, decimal? Quantity, string? Quality);
