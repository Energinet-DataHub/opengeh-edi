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
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.Edi.Responses;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

public class WholesaleServicesRequestAcceptedBuilder
{
    private readonly WholesaleServicesProcess _process;

    public WholesaleServicesRequestAcceptedBuilder(WholesaleServicesProcess process)
    {
        _process = process;
    }

    public WholesaleServicesRequestAccepted Build()
    {
        List<WholesaleServicesRequestSeries.Types.Point> timeSeriesPoints = new();
        var currentTime = InstantPattern.General.Parse(_process.StartOfPeriod).Value;
        while (currentTime < InstantPattern.General.Parse(_process.EndOfPeriod!).Value)
        {
            var quantity = new DecimalValue() { Units = currentTime.ToUnixTimeSeconds(), Nanos = 123450000, };
            var price = new DecimalValue() { Units = currentTime.ToUnixTimeSeconds(), Nanos = 123450000, };
            var amount = new DecimalValue() { Units = currentTime.ToUnixTimeSeconds(), Nanos = 123450000, };
            timeSeriesPoints.Add(new WholesaleServicesRequestSeries.Types.Point()
            {
                Quantity = quantity,
                Time = currentTime.ToTimestamp(),
                Price = price,
                Amount = amount,
                QuantityQualities = { QuantityQuality.Calculated },
            });
            currentTime = currentTime.Plus(NodaTime.Duration.FromMinutes(15));
        }

        var wholesaleServicesRequestSeries = new WholesaleServicesRequestSeries()
        {
            MeteringPointType = WholesaleServicesRequestSeries.Types.MeteringPointType.Production,
            Resolution = WholesaleServicesRequestSeries.Types.Resolution.Day,
            ChargeType = WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
            QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
            CalculationType = WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing,
            SettlementMethod = WholesaleServicesRequestSeries.Types.SettlementMethod.Flex,
            Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
            ChargeOwnerId = _process.ChargeOwner,
            EnergySupplierId = _process.EnergySupplierId,
            GridArea = _process.GridAreaCode,
            ChargeCode = "EA-001",
            Period = new Period()
            {
                StartOfPeriod = InstantPattern.General.Parse(_process.StartOfPeriod).Value.ToTimestamp(),
                EndOfPeriod = InstantPattern.General.Parse(_process.EndOfPeriod!).Value.ToTimestamp(),
            },
            CalculationResultVersion = 1,
        };

        wholesaleServicesRequestSeries.TimeSeriesPoints.AddRange(timeSeriesPoints.OrderBy(_ => Guid.NewGuid()));
        var wholesaleServicesRequestAccepted = new WholesaleServicesRequestAccepted();
        wholesaleServicesRequestAccepted.Series.Add(wholesaleServicesRequestSeries);
        return wholesaleServicesRequestAccepted;
    }

    public WholesaleServicesRequestAccepted BuildMonthlySum()
    {
        List<WholesaleServicesRequestSeries.Types.Point> timeSeriesPoints = new();
        var currentTime = InstantPattern.General.Parse(_process.StartOfPeriod).Value;
        while (currentTime < InstantPattern.General.Parse(_process.EndOfPeriod!).Value)
        {
            var amount = new DecimalValue() { Units = currentTime.ToUnixTimeSeconds(), Nanos = 123450000, };
            timeSeriesPoints.Add(new WholesaleServicesRequestSeries.Types.Point()
            {
                Time = currentTime.ToTimestamp(),
                Amount = amount,
            });
            currentTime = currentTime.Plus(NodaTime.Duration.FromMinutes(15));
        }

        // TO SPECIFIED ON MONTHLY SUM
        // MeteringPointType;
        // SettlementMethod;
        // TimeSeriesPoints[x].Quantity
        // TimeSeriesPoints[x].QuantityQualities
        // TimeSeriesPoints[x].Price
        var wholesaleServicesRequestSeries = new WholesaleServicesRequestSeries()
        {
            Resolution = WholesaleServicesRequestSeries.Types.Resolution.Monthly,
            ChargeType = WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
            QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
            CalculationType = WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing,
            Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
            ChargeOwnerId = _process.ChargeOwner,
            EnergySupplierId = _process.EnergySupplierId,
            GridArea = _process.GridAreaCode,
            ChargeCode = "EA-001",
            Period = new Period()
            {
                StartOfPeriod = InstantPattern.General.Parse(_process.StartOfPeriod).Value.ToTimestamp(),
                EndOfPeriod = InstantPattern.General.Parse(_process.EndOfPeriod!).Value.ToTimestamp(),
            },
            CalculationResultVersion = 1,
        };

        wholesaleServicesRequestSeries.TimeSeriesPoints.AddRange(timeSeriesPoints.OrderBy(_ => Guid.NewGuid()));
        var wholesaleServicesRequestAccepted = new WholesaleServicesRequestAccepted();
        wholesaleServicesRequestAccepted.Series.Add(wholesaleServicesRequestSeries);
        return wholesaleServicesRequestAccepted;
    }
}
