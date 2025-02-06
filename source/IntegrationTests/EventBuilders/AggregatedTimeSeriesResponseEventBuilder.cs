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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Duration = NodaTime.Duration;
using Period = Energinet.DataHub.Edi.Responses.Period;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
internal static class AggregatedTimeSeriesResponseEventBuilder
{
    public static AggregatedTimeSeriesRequestAccepted GenerateAcceptedFrom(
        AggregatedTimeSeriesRequest request, Instant now, IReadOnlyCollection<string>? defaultGridAreas = null)
    {
        if (request.GridAreaCodes.Count == 0 && defaultGridAreas == null)
            throw new ArgumentNullException(nameof(defaultGridAreas), "defaultGridAreas must be set when request has no GridAreaCodes");

        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(defaultGridAreas!);

        var series = gridAreas
            .Select(gridArea =>
            {
                var periodStart = InstantPattern.General.Parse(request.Period.Start).Value;
                var periodEnd = InstantPattern.General.Parse(request.Period.End).Value;

                var timeSeriesType = GetTimeSeriesType(
                    request.HasSettlementMethod ? request.SettlementMethod : null,
                    !string.IsNullOrEmpty(request.MeteringPointType) ? request.MeteringPointType : null);
                var resolution = Resolution.Pt1H;
                var points = CreatePoints(resolution, periodStart, periodEnd);

                var series = new Series
                {
                    GridArea = gridArea,
                    QuantityUnit = QuantityUnit.Kwh,
                    TimeSeriesType = timeSeriesType,
                    Resolution = resolution,
                    CalculationResultVersion = now.ToUnixTimeTicks(),
                    Period = new Period
                    {
                        StartOfPeriod = periodStart.ToTimestamp(),
                        EndOfPeriod = periodEnd.ToTimestamp(),
                    },
                    TimeSeriesPoints = { points },
                };

                if (request.BusinessReason == DataHubNames.BusinessReason.Correction)
                {
                    series.SettlementVersion = request.SettlementVersion switch
                    {
                        DataHubNames.SettlementVersion.FirstCorrection => SettlementVersion.FirstCorrection,
                        DataHubNames.SettlementVersion.SecondCorrection => SettlementVersion.SecondCorrection,
                        DataHubNames.SettlementVersion.ThirdCorrection => SettlementVersion.ThirdCorrection,
                        _ => SettlementVersion.ThirdCorrection,
                    };
                }

                return series;
            });

        var acceptedResponse = new AggregatedTimeSeriesRequestAccepted
        {
            Series = { series },
        };

        return acceptedResponse;
    }

    public static AggregatedTimeSeriesRequestRejected GenerateRejectedFrom(AggregatedTimeSeriesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rejectedResponse = new AggregatedTimeSeriesRequestRejected();

        var start = InstantPattern.General.Parse(request.Period.Start).Value;
        var end = InstantPattern.General.Parse(request.Period.End).Value;
        if (end <= start)
        {
            rejectedResponse.RejectReasons.Add(new RejectReason
            {
                ErrorCode = "E17",
                ErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse med en balancefiksering eller korrektioner / It is only possible to request data for a full month in relation to balancefixing or corrections",
            });
        }
        else
        {
            throw new NotImplementedException("Cannot generate rejected message for request");
        }

        return rejectedResponse;
    }

    private static TimeSeriesType GetTimeSeriesType(string? settlementMethodName, string? meteringPointTypeName)
    {
        return (PMTypes.SettlementMethod.FromNameOrDefault(settlementMethodName), PMTypes.MeteringPointType.FromNameOrDefault(meteringPointTypeName)) switch
        {
            (var sm, var mpt) when sm == PMTypes.SettlementMethod.Flex && mpt == PMTypes.MeteringPointType.Consumption => TimeSeriesType.FlexConsumption,
            (var sm, var mpt) when sm == PMTypes.SettlementMethod.NonProfiled && mpt == PMTypes.MeteringPointType.Consumption => TimeSeriesType.NonProfiledConsumption,
            (null, var mpt) when mpt == PMTypes.MeteringPointType.Production => TimeSeriesType.Production,
            (null, null) => TimeSeriesType.FlexConsumption, // Default if no settlement method or metering point type is set
            _ => throw new NotImplementedException($"Not implemented combination of SettlementMethod and MeteringPointType ({settlementMethodName} and {meteringPointTypeName})"),
        };
    }

    private static List<TimeSeriesPoint> CreatePoints(Resolution resolution, Instant periodStart, Instant periodEnd)
    {
        var resolutionDuration = resolution switch
        {
            Resolution.Pt1H => Duration.FromHours(1),
            Resolution.Pt15M => Duration.FromMinutes(15),
            _ => throw new NotImplementedException($"Unsupported resolution in request: {resolution}"),
        };

        var points = new List<TimeSeriesPoint>();
        var currentTime = periodStart;
        while (currentTime < periodEnd)
        {
            points.Add(CreatePoint(currentTime, quantityFactor: resolution == Resolution.Pt1H ? 4 : 1));
            currentTime = currentTime.Plus(resolutionDuration);
        }

        return points;
    }

    private static TimeSeriesPoint CreatePoint(Instant currentTime, int quantityFactor = 1)
    {
        // Create random quantity between 1.00 and 999.999 (multiplied a factor used by by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 999) };

        return new TimeSeriesPoint
        {
            Time = currentTime.ToTimestamp(),
            Quantity = quantity,
            QuantityQualities = { QuantityQuality.Estimated },
        };
    }
}
