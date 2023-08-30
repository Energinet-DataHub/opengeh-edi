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

using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.Events.DecimalValue;
using QuantityQuality = Energinet.DataHub.Wholesale.Contracts.Events.QuantityQuality;
using QuantityUnit = Energinet.DataHub.Wholesale.Contracts.Events.QuantityUnit;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;
using TimeSeriesPoint = Energinet.DataHub.Wholesale.Contracts.Events.TimeSeriesPoint;
using TimeSeriesType = Energinet.DataHub.Wholesale.Contracts.Events.TimeSeriesType;

namespace AcceptanceTest.Drivers;

internal sealed class WholeSaleDriver
{
    private readonly IntegrationEventPublisher _integrationEventPublisher;
    private readonly InboxPublisher _inboxPublisher;

    internal WholeSaleDriver(IntegrationEventPublisher integrationEventPublisher, InboxPublisher inboxPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _inboxPublisher = inboxPublisher;
    }

    internal Task PublishAggregationResultAsync(string gridAreaCode)
    {
        return _integrationEventPublisher.PublishAsync(
            "CalculationResultCompleted",
            CreateAggregationResultAvailableEventFor(gridAreaCode).ToByteArray());
    }

    internal Task PublishToInboxAsync()
    {
        return _inboxPublisher.SendToInboxAsync(
            "AggregatedMeasureDataAccepted",
            CreateAggregationMeasureDataAccepted().ToByteArray());
    }

    private static IMessage CreateAggregationMeasureDataAccepted()
    {
        return new AggregatedTimeSeriesRequestAccepted();
    }

    private static CalculationResultCompleted CreateAggregationResultAvailableEventFor(string gridAreaCode)
    {
        var processCompletedEvent = new CalculationResultCompleted()
        {
            Resolution = Resolution.Quarter,
            QuantityUnit = QuantityUnit.Kwh,
            ProcessType = ProcessType.BalanceFixing,
            TimeSeriesType = TimeSeriesType.Production,
            BatchId = Guid.NewGuid().ToString(),
            AggregationPerGridarea = new AggregationPerGridArea()
            {
                GridAreaCode = gridAreaCode,
            },
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            TimeSeriesPoints = { new TimeSeriesPoint { Time = new Timestamp { Seconds = 100000 }, Quantity = new DecimalValue { Units = 123, Nanos = 1200000 }, QuantityQuality = QuantityQuality.Measured } },
        };
        return processCompletedEvent;
    }
}
