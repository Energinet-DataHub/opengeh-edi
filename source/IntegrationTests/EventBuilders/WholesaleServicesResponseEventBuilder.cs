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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

public static class WholesaleServicesResponseEventBuilder
{
    /// <summary>
    /// Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
    /// It is very important that the generated data is correct, since assertions is based on this data
    /// </summary>
    public static WholesaleServicesRequestAccepted GenerateWholesaleServicesRequestAccepted(WholesaleServicesRequest request, Instant now, string? defaultChargeOwnerId = null, ICollection<string>? defaultGridAreas = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(now);

        if (!request.HasChargeOwnerId && defaultChargeOwnerId == null)
            throw new ArgumentNullException(nameof(defaultChargeOwnerId), "defaultChargeOwnerId must be set when request has null ChargeOwnerId");
        if (request.GridAreaCodes.Count == 0 && defaultGridAreas == null)
            throw new ArgumentNullException(nameof(defaultGridAreas), "defaultGridAreas must be set when request has no GridAreaCodes");

        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(defaultGridAreas!);

        var chargeTypes = request.ChargeTypes;
        if (chargeTypes.Count == 0)
            chargeTypes.Add(new ChargeType { ChargeCode = "12345678", ChargeType_ = DataHubNames.ChargeType.Tariff });

        var series = gridAreas.SelectMany(
            ga => chargeTypes.Select(ct =>
            {
                var resolution = request.Resolution == DataHubNames.Resolution.Monthly
                    ? WholesaleServicesRequestSeries.Types.Resolution.Monthly
                    : WholesaleServicesRequestSeries.Types.Resolution.Hour;

                var periodStart = InstantPattern.General.Parse(request.PeriodStart).Value;
                var periodEnd = InstantPattern.General.Parse(request.PeriodEnd).Value;

                var points = CreatePoints(resolution, periodEnd, periodStart);

                var series = new WholesaleServicesRequestSeries()
                {
                    Currency = WholesaleServicesRequestSeries.Types.Currency.Dkk,
                    Period = new Period
                    {
                        StartOfPeriod = periodStart.ToTimestamp(),
                        EndOfPeriod = periodEnd.ToTimestamp(),
                    },
                    Resolution = resolution,
                    CalculationType = request.BusinessReason == DataHubNames.BusinessReason.WholesaleFixing
                        ? WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing
                        : throw new NotImplementedException("Builder only supports WholesaleFixing, not corrections"),
                    ChargeCode = ct.ChargeCode,
                    ChargeType =
                        Enum.TryParse<WholesaleServicesRequestSeries.Types.ChargeType>(ct.ChargeType_, out var result)
                            ? result
                            : throw new NotImplementedException("Unsupported chargetype in request"),
                    ChargeOwnerId = request.HasChargeOwnerId ? request.ChargeOwnerId : defaultChargeOwnerId!,
                    GridArea = ga,
                    QuantityUnit = WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh,
                    SettlementMethod = WholesaleServicesRequestSeries.Types.SettlementMethod.Flex,
                    EnergySupplierId = request.EnergySupplierId,
                    MeteringPointType = WholesaleServicesRequestSeries.Types.MeteringPointType.Consumption,
                    CalculationResultVersion = now.ToUnixTimeTicks(),
                };

                series.TimeSeriesPoints.AddRange(points);

                return series;
            }));

        var requestAcceptedMessage = new WholesaleServicesRequestAccepted();
        requestAcceptedMessage.Series.AddRange(series);

        return requestAcceptedMessage;
    }

    public static WholesaleServicesRequestRejected GenerateWholesaleServicesRequestRejected(WholesaleServicesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rejectedMessage = new WholesaleServicesRequestRejected();

        var start = InstantPattern.General.Parse(request.PeriodStart).Value;
        var end = InstantPattern.General.Parse(request.PeriodEnd).Value;
        if (end <= start)
        {
            rejectedMessage.RejectReasons.Add(new RejectReason
            {
                ErrorCode = "E17",
                ErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse med en engrosfiksering eller korrektioner / It is only possible to request data for a full month in relation to wholesalefixing or corrections",
            });
        }
        else
        {
            throw new NotImplementedException("Cannot generate rejected message for request");
        }

        return rejectedMessage;
    }

    private static List<WholesaleServicesRequestSeries.Types.Point> CreatePoints(WholesaleServicesRequestSeries.Types.Resolution resolution, Instant periodEnd, Instant periodStart)
    {
        var points = new List<WholesaleServicesRequestSeries.Types.Point>();

        if (resolution == WholesaleServicesRequestSeries.Types.Resolution.Monthly)
        {
            points.Add(CreatePoint(periodEnd, quantityFactor: 30 * 24));
        }
        else
        {
            var resolutionDuration = resolution switch
            {
                WholesaleServicesRequestSeries.Types.Resolution.Day => Duration.FromHours(24),
                WholesaleServicesRequestSeries.Types.Resolution.Hour => Duration.FromHours(1),
                _ => throw new NotImplementedException($"Unsupported resolution in request: {resolution.ToString()}"),
            };

            var currentTime = periodStart;
            while (currentTime < periodEnd)
            {
                points.Add(CreatePoint(currentTime));
                currentTime = currentTime.Plus(resolutionDuration);
            }
        }

        return points;
    }

    [SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
    private static WholesaleServicesRequestSeries.Types.Point CreatePoint(Instant currentTime, int quantityFactor = 1)
    {
        // Create random price between 0.99 and 5.99
        var price = new DecimalValue { Units = Random.Shared.Next(0, 4), Nanos = Random.Shared.Next(1, 99) };

        // Create random quantity between 1.00 and 999.99 (multiplied a factor used by by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 99) };

        // Calculate the total amount (price * quantity)
        var totalAmount = price.ToDecimal() * quantity.ToDecimal();

        return new WholesaleServicesRequestSeries.Types.Point
        {
            Time = currentTime.ToTimestamp(),
            Price = price,
            Quantity = quantity,
            Amount = DecimalValue.FromDecimal(totalAmount),
            QuantityQualities = { QuantityQuality.Calculated },
        };
    }
}
