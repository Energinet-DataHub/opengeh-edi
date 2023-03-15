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
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;

namespace IntegrationTests.Factories;

internal sealed class CalculationResultCompletedEventBuilder
{
    private ProcessType _processType = ProcessType.BalanceFixing;
    private Resolution _resolution = Resolution.Quarter;

    internal CalculationResultCompleted Build()
    {
        return new CalculationResultCompleted()
        {
            ProcessType = _processType,
            Resolution = _resolution,
            BatchId = Guid.NewGuid().ToString(),
            QuantityUnit = QuantityUnit.Kwh,
            AggregationPerGridarea = new AggregationPerGridArea() { GridAreaCode = "805", },
            PeriodStartUtc = Timestamp.FromDateTime(DateTime.UtcNow),
            PeriodEndUtc = Timestamp.FromDateTime(DateTime.UtcNow),
            TimeSeriesType = TimeSeriesType.FlexConsumption,
            TimeSeriesPoints =
            {
                new TimeSeriesPoint()
                {
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    Quantity = new DecimalValue() { Nanos = 1, Units = 1 },
                    QuantityQuality = QuantityQuality.Measured,
                },
            },
        };
    }

    internal CalculationResultCompletedEventBuilder WithProcessType(ProcessType processType)
    {
        _processType = processType;
        return this;
    }

    internal CalculationResultCompletedEventBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }
}
