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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Edi.Mappers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NodaTime.Serialization.Protobuf;
using ATS = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults.AggregatedTimeSeries;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.Wholesale.Edi.Factories.AggregatedTimeSeries;

public static class AggregatedTimeSeriesRequestAcceptedMessageFactory
{
    public static ServiceBusMessage Create(IReadOnlyCollection<ATS> aggregatedTimeSeries, string referenceId)
    {
        var body = CreateAcceptedResponse(aggregatedTimeSeries);

        var message = new ServiceBusMessage
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
        };

        message.ApplicationProperties.Add("ReferenceId", referenceId);
        return message;
    }

    private static AggregatedTimeSeriesRequestAccepted CreateAcceptedResponse(IReadOnlyCollection<ATS> aggregatedTimeSeries)
    {
        var response = new AggregatedTimeSeriesRequestAccepted();
        foreach (var series in aggregatedTimeSeries)
        {
            var points = CreateTimeSeriesPoints(series);
            var seriesToAdd = new Series
            {
                GridArea = series.GridArea,
                QuantityUnit = QuantityUnit.Kwh,
                TimeSeriesPoints = { points },
                TimeSeriesType = TimeSeriesType.Unspecified,
                Resolution = ResolutionMapper.Map(series.Resolution),
                CalculationResultVersion = series.Version,
                Period = new Period()
                {
                    StartOfPeriod = series.PeriodStart.ToTimestamp(),
                    EndOfPeriod = series.PeriodEnd.ToTimestamp(),
                },
            };

            var settlementVersion = GetSettlementVersion(series);
            if (settlementVersion != null)
            {
                seriesToAdd.SettlementVersion = settlementVersion.Value;
            }

            response.Series.Add(seriesToAdd);
        }

        return response;
    }

    private static IReadOnlyCollection<TimeSeriesPoint> CreateTimeSeriesPoints(ATS aggregatedTimeSeries)
    {
        var points = new List<TimeSeriesPoint>();
        foreach (var timeSeriesPoint in aggregatedTimeSeries.TimeSeriesPoints)
        {
            var point = new TimeSeriesPoint
            {
                Quantity = DecimalValueMapper.Map(timeSeriesPoint.Quantity),
                Time = new Timestamp { Seconds = timeSeriesPoint.Time.ToUnixTimeSeconds(), },
            };
            point.QuantityQualities.AddRange(timeSeriesPoint.Qualities.Select(QuantityQualityMapper.MapQuantityQuality));

            points.Add(point);
        }

        return points;
    }

    private static SettlementVersion? GetSettlementVersion(ATS series)
    {
        if (series.SettlementVersion == null)
        {
            return null;
        }

        var settlementVersionMap = new Dictionary<Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion, SettlementVersion>
        {
            { EDI.BuildingBlocks.Domain.Models.SettlementVersion.FirstCorrection, SettlementVersion.FirstCorrection },
            { EDI.BuildingBlocks.Domain.Models.SettlementVersion.SecondCorrection, SettlementVersion.SecondCorrection },
            { EDI.BuildingBlocks.Domain.Models.SettlementVersion.ThirdCorrection, SettlementVersion.ThirdCorrection },
        };

        if (settlementVersionMap.TryGetValue(series.SettlementVersion, out var settlementVersion))
        {
            return settlementVersion;
        }

        throw new ArgumentOutOfRangeException(
            nameof(series.SettlementVersion),
            actualValue: series.SettlementVersion,
            "Value does not contain a valid SettlementVersion.");
    }
}
