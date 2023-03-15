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
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Duration = NodaTime.Duration;

namespace IntegrationTests.Factories;

internal sealed class CalculationResultCompletedEventBuilder
{
    private ProcessType _processType = ProcessType.BalanceFixing;
    private Resolution _resolution = Resolution.Quarter;
    private QuantityUnit _measurementUnit = QuantityUnit.Kwh;
    private AggregationPerGridArea? _aggregationPerGridArea;
    private AggregationPerEnergySupplierPerGridArea? _aggregationPerEnergySupplier;
    private AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea? _aggregationPerBalanceResponsiblePerEnergySupplier;
    private Timestamp _startOfPeriod = SystemClock.Instance.GetCurrentInstant().ToTimestamp();
    private Timestamp _endOfPeriod = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1)).ToTimestamp();
    private TimeSeriesType _timeSeriesType = TimeSeriesType.NonProfiledConsumption;
    private List<TimeSeriesPoint> _timeSeriesPoints = new List<TimeSeriesPoint>()
    {
        new TimeSeriesPoint()
        {
            Time = Timestamp.FromDateTime(DateTime.UtcNow),
            Quantity = new DecimalValue() { Nanos = 1, Units = 1 },
            QuantityQuality = QuantityQuality.Measured,
        },
    };

    internal CalculationResultCompleted Build()
    {
        CalculationResultCompleted @event;
        if (_aggregationPerGridArea is not null)
        {
            @event = new CalculationResultCompleted()
            {
                ProcessType = _processType,
                Resolution = _resolution,
                BatchId = Guid.NewGuid().ToString(),
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
            @event = new CalculationResultCompleted()
            {
                ProcessType = _processType,
                Resolution = _resolution,
                BatchId = Guid.NewGuid().ToString(),
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
            @event = new CalculationResultCompleted()
            {
                ProcessType = _processType,
                Resolution = _resolution,
                BatchId = Guid.NewGuid().ToString(),
                QuantityUnit = _measurementUnit,
                AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea = _aggregationPerBalanceResponsiblePerEnergySupplier,
                PeriodStartUtc = _startOfPeriod,
                PeriodEndUtc = _endOfPeriod,
                TimeSeriesType = _timeSeriesType,
            };
            @event.TimeSeriesPoints.Add(_timeSeriesPoints);
            return @event;
        }

        @event = new CalculationResultCompleted()
        {
            ProcessType = _processType,
            Resolution = _resolution,
            BatchId = Guid.NewGuid().ToString(),
            QuantityUnit = _measurementUnit,
            PeriodStartUtc = _startOfPeriod,
            PeriodEndUtc = _endOfPeriod,
            TimeSeriesType = _timeSeriesType,
        };
        @event.TimeSeriesPoints.Add(_timeSeriesPoints);
        return @event;
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

    internal CalculationResultCompletedEventBuilder WithMeasurementUnit(QuantityUnit measurementUnit)
    {
        _measurementUnit = measurementUnit;
        return this;
    }

    internal CalculationResultCompletedEventBuilder AggregatedBy(string gridAreaCode, string? balanceResponsibleNumber = null, string? energySupplierNumber = null)
    {
        _aggregationPerGridArea = null;
        _aggregationPerEnergySupplier = null;
        _aggregationPerBalanceResponsiblePerEnergySupplier = null;

        if (balanceResponsibleNumber is null && energySupplierNumber is null)
        {
            _aggregationPerGridArea = new AggregationPerGridArea() { GridAreaCode = gridAreaCode, };
        }

        if (energySupplierNumber is not null && balanceResponsibleNumber is null)
        {
            _aggregationPerEnergySupplier = new AggregationPerEnergySupplierPerGridArea()
            {
                GridAreaCode = gridAreaCode, EnergySupplierGlnOrEic = energySupplierNumber,
            };
        }

        if (balanceResponsibleNumber is not null && energySupplierNumber is not null)
        {
            _aggregationPerBalanceResponsiblePerEnergySupplier =
                new AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea()
                {
                    GridAreaCode = gridAreaCode,
                    EnergySupplierGlnOrEic = energySupplierNumber,
                    BalanceResponsiblePartyGlnOrEic = balanceResponsibleNumber,
                };
        }

        return this;
    }

    internal CalculationResultCompletedEventBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _startOfPeriod = startOfPeriod.ToTimestamp();
        _endOfPeriod = endOfPeriod.ToTimestamp();
        return this;
    }

    internal CalculationResultCompletedEventBuilder ResultOf(TimeSeriesType timeSeriesType)
    {
        _timeSeriesType = timeSeriesType;
        return this;
    }
}
