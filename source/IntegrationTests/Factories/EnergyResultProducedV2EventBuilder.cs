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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;
using Duration = NodaTime.Duration;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal sealed class EnergyResultProducedV2EventBuilder
{
    private readonly List<TimeSeriesPoint> _timeSeriesPoints = new();
    private CalculationType _calculationType = CalculationType.BalanceFixing;
    private Resolution _resolution = Resolution.Quarter;
    private QuantityUnit _measurementUnit = QuantityUnit.Kwh;
    private AggregationPerGridArea? _aggregationPerGridArea;
    private AggregationPerEnergySupplierPerGridArea? _aggregationPerEnergySupplier;

    private IEnumerable<QuantityQuality> _quantityQualities = new List<QuantityQuality> { QuantityQuality.Measured };

    private AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea?
        _aggregationPerBalanceResponsiblePerEnergySupplier;

    private AggregationPerBalanceResponsiblePartyPerGridArea? _aggregationPerBalanceResponsible;
    private Timestamp _startOfPeriod = SystemClock.Instance.GetCurrentInstant().ToTimestamp();
    private Timestamp _endOfPeriod = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1)).ToTimestamp();

    private TimeSeriesType _timeSeriesType =
        TimeSeriesType.NonProfiledConsumption;

    internal EnergyResultProducedV2 Build()
    {
        if (_timeSeriesPoints.Count == 0)
        {
            _timeSeriesPoints.Add(
               new TimeSeriesPoint
                    {
                        Time = Timestamp.FromDateTime(DateTime.UtcNow),
                        Quantity = new DecimalValue { Nanos = 1, Units = 1 },
                        QuantityQualities = { _quantityQualities },
                    });
        }

        EnergyResultProducedV2 @event;
        if (_aggregationPerGridArea is not null)
        {
            @event = new EnergyResultProducedV2
            {
                CalculationType = _calculationType,
                Resolution = _resolution,
                CalculationId = Guid.NewGuid().ToString(),
                QuantityUnit = _measurementUnit,
                AggregationPerGridarea = _aggregationPerGridArea,
                PeriodStartUtc = _startOfPeriod,
                PeriodEndUtc = _endOfPeriod,
                TimeSeriesType = _timeSeriesType,
            };
            @event.TimeSeriesPoints.Add(_timeSeriesPoints);
            return @event;
        }

        if (_aggregationPerEnergySupplier is not null)
        {
            @event = new EnergyResultProducedV2
            {
                CalculationType = _calculationType,
                Resolution = _resolution,
                CalculationId = Guid.NewGuid().ToString(),
                QuantityUnit = _measurementUnit,
                AggregationPerEnergysupplierPerGridarea = _aggregationPerEnergySupplier,
                PeriodStartUtc = _startOfPeriod,
                PeriodEndUtc = _endOfPeriod,
                TimeSeriesType = _timeSeriesType,
            };
            @event.TimeSeriesPoints.Add(_timeSeriesPoints);
            return @event;
        }

        if (_aggregationPerBalanceResponsiblePerEnergySupplier is not null)
        {
            @event = new EnergyResultProducedV2
            {
                CalculationType = _calculationType,
                Resolution = _resolution,
                CalculationId = Guid.NewGuid().ToString(),
                QuantityUnit = _measurementUnit,
                AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea = _aggregationPerBalanceResponsiblePerEnergySupplier,
                PeriodStartUtc = _startOfPeriod,
                PeriodEndUtc = _endOfPeriod,
                TimeSeriesType = _timeSeriesType,
            };
            @event.TimeSeriesPoints.Add(_timeSeriesPoints);
            return @event;
        }

        if (_aggregationPerBalanceResponsible is not null)
        {
            @event = new EnergyResultProducedV2
            {
                CalculationType = _calculationType,
                Resolution = _resolution,
                CalculationId = Guid.NewGuid().ToString(),
                QuantityUnit = _measurementUnit,
                AggregationPerBalanceresponsiblepartyPerGridarea = _aggregationPerBalanceResponsible,
                PeriodStartUtc = _startOfPeriod,
                PeriodEndUtc = _endOfPeriod,
                TimeSeriesType = _timeSeriesType,
            };
            @event.TimeSeriesPoints.Add(_timeSeriesPoints);
            return @event;
        }

        @event = new EnergyResultProducedV2
        {
            CalculationType = _calculationType,
            Resolution = _resolution,
            CalculationId = Guid.NewGuid().ToString(),
            QuantityUnit = _measurementUnit,
            PeriodStartUtc = _startOfPeriod,
            PeriodEndUtc = _endOfPeriod,
            TimeSeriesType = _timeSeriesType,
        };
        @event.TimeSeriesPoints.Add(_timeSeriesPoints);
        return @event;
    }

    internal EnergyResultProducedV2EventBuilder WithCalculationType(CalculationType calculationType)
    {
        _calculationType = calculationType;
        return this;
    }

    internal EnergyResultProducedV2EventBuilder WithResolution(Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    internal EnergyResultProducedV2EventBuilder WithMeasurementUnit(QuantityUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    internal EnergyResultProducedV2EventBuilder WithPointsForPeriod()
    {
        var currentTime = _startOfPeriod.ToInstant();
        while (currentTime < _endOfPeriod.ToInstant())
        {
            _timeSeriesPoints.Add(new()
            {
                Time = Timestamp.FromDateTime(DateTime.UtcNow),
                Quantity = new DecimalValue { Nanos = 1, Units = 1 },
                QuantityQualities = { _quantityQualities },
            });
            currentTime = currentTime.Plus(Duration.FromMinutes(15));
        }

        return this;
    }

    internal EnergyResultProducedV2EventBuilder AggregatedBy(string gridAreaCode, string? balanceResponsibleNumber = null, string? energySupplierNumber = null)
    {
        _aggregationPerGridArea = null;
        _aggregationPerEnergySupplier = null;
        _aggregationPerBalanceResponsiblePerEnergySupplier = null;
        _aggregationPerBalanceResponsible = null;

        if (balanceResponsibleNumber is null && energySupplierNumber is null)
        {
            _aggregationPerGridArea =
                new AggregationPerGridArea { GridAreaCode = gridAreaCode };
        }

        if (energySupplierNumber is not null && balanceResponsibleNumber is null)
        {
            _aggregationPerEnergySupplier = new AggregationPerEnergySupplierPerGridArea
            {
                GridAreaCode = gridAreaCode, EnergySupplierId = energySupplierNumber,
            };
        }

        if (balanceResponsibleNumber is not null && energySupplierNumber is not null)
        {
            _aggregationPerBalanceResponsiblePerEnergySupplier =
                new AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea
                {
                    GridAreaCode = gridAreaCode,
                    EnergySupplierId = energySupplierNumber,
                    BalanceResponsibleId = balanceResponsibleNumber,
                };
        }

        if (balanceResponsibleNumber is not null && energySupplierNumber is null)
        {
            _aggregationPerBalanceResponsible =
                new AggregationPerBalanceResponsiblePartyPerGridArea
                {
                    GridAreaCode = gridAreaCode, BalanceResponsibleId = balanceResponsibleNumber,
                };
        }

        return this;
    }

    internal EnergyResultProducedV2EventBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _startOfPeriod = startOfPeriod.ToTimestamp();
        _endOfPeriod = endOfPeriod.ToTimestamp();
        return this;
    }

    internal EnergyResultProducedV2EventBuilder ResultOf(TimeSeriesType timeSeriesType)
    {
        _timeSeriesType = timeSeriesType;
        return this;
    }

    internal EnergyResultProducedV2EventBuilder WithQuantityQualities(IEnumerable<QuantityQuality> quantityQualities)
    {
        _quantityQualities = quantityQualities;
        return this;
    }
}
