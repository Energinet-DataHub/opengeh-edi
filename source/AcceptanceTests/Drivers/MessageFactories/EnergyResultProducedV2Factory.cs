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

using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.MessageFactories;

internal static class EnergyResultProducedV2Factory
{
    public static EnergyResultProducedV2 CreateAggregationResultAvailableEventFor(string gridAreaCode)
    {
        var processCompletedEvent = new EnergyResultProducedV2
        {
            Resolution = EnergyResultProducedV2.Types.Resolution.Quarter,
            QuantityUnit = EnergyResultProducedV2.Types.QuantityUnit.Kwh,
            CalculationType = EnergyResultProducedV2.Types.CalculationType.WholesaleFixing, // Use WholesaleFixing since BalanceFixing uses CalculationCompletedEvent handling
            TimeSeriesType = EnergyResultProducedV2.Types.TimeSeriesType.Production,
            CalculationId = Guid.NewGuid().ToString(),
            AggregationPerGridarea =
                new EnergyResultProducedV2.Types.AggregationPerGridArea { GridAreaCode = gridAreaCode },
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            TimeSeriesPoints =
            {
                new EnergyResultProducedV2.Types.TimeSeriesPoint
                {
                    Time = new Timestamp { Seconds = 100000 },
                    Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
                    QuantityQualities = { EnergyResultProducedV2.Types.QuantityQuality.Measured },
                },
            },
            CalculationResultVersion = 404,
        };
        return processCompletedEvent;
    }

    public static EnergyResultProducedV2 CreateAggregationResultAvailableEventForBalanceResponsible(
        string gridAreaCode,
        string actorNumber)
    {
        var processCompletedEvent = new EnergyResultProducedV2
        {
            Resolution = EnergyResultProducedV2.Types.Resolution.Quarter,
            QuantityUnit = EnergyResultProducedV2.Types.QuantityUnit.Kwh,
            CalculationType = EnergyResultProducedV2.Types.CalculationType.WholesaleFixing, // Use WholesaleFixing since BalanceFixing uses CalculationCompletedEvent handling
            TimeSeriesType = EnergyResultProducedV2.Types.TimeSeriesType.Production,
            CalculationId = Guid.NewGuid().ToString(),
            AggregationPerBalanceresponsiblepartyPerGridarea =
                new EnergyResultProducedV2.Types.AggregationPerBalanceResponsiblePartyPerGridArea
                {
                    GridAreaCode = gridAreaCode, BalanceResponsibleId = actorNumber,
                },
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            TimeSeriesPoints =
            {
                new EnergyResultProducedV2.Types.TimeSeriesPoint
                {
                    Time = new Timestamp { Seconds = 100000 },
                    Quantity = new DecimalValue { Units = 123, Nanos = 1200000 },
                    QuantityQualities = { EnergyResultProducedV2.Types.QuantityQuality.Measured },
                },
            },
        };
        return processCompletedEvent;
    }
}
