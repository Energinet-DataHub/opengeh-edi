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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Duration = NodaTime.Duration;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public class AmountPerChargeResultProducedV1EventBuilder
{
    private readonly List<(long Sum, long Quantity, int Position, long? Price, AmountPerChargeResultProducedV1.Types.QuantityQuality Quality)> _points = new();
    private AmountPerChargeResultProducedV1.Types.MeteringPointType _meteringPointType = AmountPerChargeResultProducedV1.Types.MeteringPointType.Production;
    private AmountPerChargeResultProducedV1.Types.SettlementMethod _settlementMethod = AmountPerChargeResultProducedV1.Types.SettlementMethod.Flex;
    private Guid _calculationId = Guid.NewGuid();
    private AmountPerChargeResultProducedV1.Types.CalculationType _calculationType = AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing;
    private Timestamp _periodStartUtc = Instant.FromUtc(2023, 10, 1, 0, 0, 0).ToTimestamp();
    private Timestamp _periodEndUtc = Instant.FromUtc(2023, 10, 2, 0, 0, 0).ToTimestamp();
    private string _gridAreaCode = "805";
    private string _energySupplier = "8200000007743";
    private string _chargeCode = "ESP-C-F-04";
    private AmountPerChargeResultProducedV1.Types.ChargeType _chargeType = AmountPerChargeResultProducedV1.Types.ChargeType.Fee;
    private string _chargeOwner = "8200000007740";
    private AmountPerChargeResultProducedV1.Types.QuantityUnit _quantityUnit = AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh;
    private bool _isTax;
    private AmountPerChargeResultProducedV1.Types.Currency _currency = AmountPerChargeResultProducedV1.Types.Currency.Dkk;
    private long _calculationVersion = 1;
    private AmountPerChargeResultProducedV1.Types.Resolution _resolution = AmountPerChargeResultProducedV1.Types.Resolution.Day;

    internal AmountPerChargeResultProducedV1 Build()
    {
        var points = BuildTimeSeriesPoints();

        var @event = new AmountPerChargeResultProducedV1
        {
            CalculationId = _calculationId.ToString(),
            CalculationType = _calculationType,
            PeriodStartUtc = _periodStartUtc,
            PeriodEndUtc = _periodEndUtc,
            GridAreaCode = _gridAreaCode,
            EnergySupplierId = _energySupplier,
            ChargeCode = _chargeCode,
            ChargeType = _chargeType,
            ChargeOwnerId = _chargeOwner,
            QuantityUnit = _quantityUnit,
            IsTax = _isTax,
            Currency = _currency,
            MeteringPointType = _meteringPointType,
            SettlementMethod = _settlementMethod,
            CalculationResultVersion = _calculationVersion,
            Resolution = _resolution,
        };

        @event.TimeSeriesPoints.AddRange(points);

        return @event;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        _calculationType = calculationType;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithCalculationVersion(long calculationVersion)
    {
        _calculationVersion = calculationVersion;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithStartOfPeriod(Timestamp startOfPeriod)
    {
        _periodStartUtc = startOfPeriod;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithEndOfPeriod(Timestamp endOfPeriod)
    {
        _periodEndUtc = endOfPeriod;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithEnergySupplier(string energySupplier)
    {
        _energySupplier = energySupplier;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithChargeCode(string chargeCode)
    {
        _chargeCode = chargeCode;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithChargeType(AmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        _chargeType = chargeType;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithChargeOwner(string chargeOwner)
    {
        _chargeOwner = chargeOwner;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithQuantityUnit(AmountPerChargeResultProducedV1.Types.QuantityUnit quantityUnit)
    {
        _quantityUnit = quantityUnit;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithIsTax(bool isTax)
    {
        _isTax = isTax;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithCurrency(AmountPerChargeResultProducedV1.Types.Currency currency)
    {
        _currency = currency;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithCalculationId(Guid calculationId)
    {
        _calculationId = calculationId;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithResolution(AmountPerChargeResultProducedV1.Types.Resolution resolution)
    {
        _resolution = resolution;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithMeteringPointType(AmountPerChargeResultProducedV1.Types.MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithSettlementMethod(AmountPerChargeResultProducedV1.Types.SettlementMethod settlementMethod)
    {
        _settlementMethod = settlementMethod;
        return this;
    }

    internal AmountPerChargeResultProducedV1EventBuilder WithPoint(int position, long? price, long quantity, long sum, AmountPerChargeResultProducedV1.Types.QuantityQuality quality)
    {
        _points.Add((sum, quantity, position, price, quality));
        return this;
    }

    private List<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint> BuildTimeSeriesPoints()
    {
        var resolutionInHours = _resolution switch
        {
            AmountPerChargeResultProducedV1.Types.Resolution.Day => 24,
            AmountPerChargeResultProducedV1.Types.Resolution.Hour => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(_resolution), _resolution, "Unhandled resolution type when building time series points"),
        };

        List<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint> timeSeriesPoints = new();
        if (_points.Count > 0)
        {
            timeSeriesPoints = _points
                .Select(p =>
                {
                    var newPoint = new AmountPerChargeResultProducedV1.Types.TimeSeriesPoint
                    {
                        Time =
                            _periodStartUtc.ToInstant()
                                .Plus(Duration.FromHours(resolutionInHours * p.Position))
                                .ToTimestamp(),
                        Quantity = new DecimalValue { Units = p.Quantity, Nanos = 0 },
                        Amount = new DecimalValue { Units = p.Sum, Nanos = 0 },
                        QuantityQualities = { p.Quality },
                    };
                    if (p.Price != null)
                        newPoint.Price = new DecimalValue { Units = p.Price.Value, Nanos = 0 };

                    return newPoint;
                })
                .ToList();
        }
        else
        {
            timeSeriesPoints = GenerateTimeSeriesPoints();
        }

        return timeSeriesPoints;
    }

    private List<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint> GenerateTimeSeriesPoints()
    {
        var resolutionInHours = _resolution switch
        {
            AmountPerChargeResultProducedV1.Types.Resolution.Day => 24,
            AmountPerChargeResultProducedV1.Types.Resolution.Hour => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(_resolution), _resolution, "Unhandled resolution type when building time series points"),
        };

        var timeSeriesPoints = new List<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint>();
        var currentTime = _periodStartUtc;
        while (currentTime < _periodEndUtc.ToInstant().Plus(Duration.FromDays(1)).ToTimestamp())
        {
            var point = new AmountPerChargeResultProducedV1.Types.TimeSeriesPoint
            {
                Amount = new DecimalValue { Units = 1, Nanos = 0 },
                Price = new DecimalValue { Units = 2, Nanos = 0 },
                Quantity = new DecimalValue { Units = 3, Nanos = 0 },
                Time = currentTime,
                QuantityQualities = { AmountPerChargeResultProducedV1.Types.QuantityQuality.Measured },
            };
            timeSeriesPoints.Add(point);

            currentTime = currentTime
                .ToInstant()
                .Plus(Duration.FromHours(resolutionInHours))
                .ToTimestamp();
        }

        return timeSeriesPoints;
    }
}
