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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class WholesaleDriver
{
    public const string BalanceResponsiblePartyMarketRoleCode = "DDK";
    private readonly IntegrationEventPublisher _integrationEventPublisher;

    internal WholesaleDriver(IntegrationEventPublisher integrationEventPublisher)
    {
        _integrationEventPublisher = integrationEventPublisher;
    }

    internal Task PublishAggregationResultAsync(string gridAreaCode, ActorRole? marketRole = null, string? actorNumber = null)
    {
        var aggregation = marketRole?.Code switch
        {
            BalanceResponsiblePartyMarketRoleCode => CreateAggregationResultAvailableEventForBalanceResponsible(gridAreaCode, actorNumber ?? throw new ArgumentNullException(nameof(actorNumber))),
            _ => CreateAggregationResultAvailableEventFor(gridAreaCode),
        };

        return _integrationEventPublisher.PublishAsync(
            EnergyResultProducedV2.EventName,
            aggregation.ToByteArray());
    }

    private static EnergyResultProducedV2 CreateAggregationResultAvailableEventFor(string gridAreaCode)
    {
        var processCompletedEvent = new EnergyResultProducedV2
        {
            Resolution = EnergyResultProducedV2.Types.Resolution.Quarter,
            QuantityUnit = QuantityUnit.Kwh,
            CalculationType = CalculationType.BalanceFixing,
            TimeSeriesType = TimeSeriesType.Production,
            CalculationId = Guid.NewGuid().ToString(),
            AggregationPerGridarea = new AggregationPerGridArea()
            {
                GridAreaCode = gridAreaCode,
            },
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            TimeSeriesPoints =
            {
                new TimeSeriesPoint
                {
                    Time = new Timestamp { Seconds = 100000 },
                    Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
                    QuantityQualities = { QuantityQuality.Measured },
                },
            },
            CalculationResultVersion = 404,
        };
        return processCompletedEvent;
    }

    private static EnergyResultProducedV2 CreateAggregationResultAvailableEventForBalanceResponsible(
        string gridAreaCode,
        string actorNumber)
    {
        var processCompletedEvent = new EnergyResultProducedV2
        {
            Resolution = EnergyResultProducedV2.Types.Resolution.Quarter,
            QuantityUnit = QuantityUnit.Kwh,
            CalculationType = CalculationType.BalanceFixing,
            TimeSeriesType = TimeSeriesType.Production,
            CalculationId = Guid.NewGuid().ToString(),
            AggregationPerBalanceresponsiblepartyPerGridarea = new AggregationPerBalanceResponsiblePartyPerGridArea
            {
                GridAreaCode = gridAreaCode, BalanceResponsibleId = actorNumber,
            },
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            TimeSeriesPoints =
            {
                new TimeSeriesPoint
                {
                    Time = new Timestamp { Seconds = 100000 },
                    Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
                    QuantityQualities = { QuantityQuality.Measured },
                },
            },
        };
        return processCompletedEvent;
    }
}
