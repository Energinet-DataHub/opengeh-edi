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
using System.IO;
using System.Threading.Tasks;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;

namespace Infrastructure.Transactions.Aggregations;

public class AggregationResultMapper
{
    private readonly ISerializer _serializer;

    public AggregationResultMapper(ISerializer serializer)
    {
        _serializer = serializer;
    }

    private enum ProcessStepMeteringPointType
    {
        Production = 0,
    }

    public async Task<AggregationResult> MapFromAsync(Stream payload, Guid resultId, string gridArea, Period period)
    {
        var resultDto = (ProcessStepResultDto)await _serializer.DeserializeAsync(payload, typeof(ProcessStepResultDto)).ConfigureAwait(false);
        return AggregationResult.Production(
            resultId,
            gridArea,
            "KWH",
            "PTH1",
            period,
            ExtractPoints(resultDto!.TimeSeriesPoints));
    }

    public async Task<AggregationResult> MapToConsumptionResultAsync(Stream payload, Guid resultId, string gridArea, Period period, SettlementType settlementType)
    {
        var resultDto = (ProcessStepResultDto)await _serializer.DeserializeAsync(payload, typeof(ProcessStepResultDto)).ConfigureAwait(false);
        return AggregationResult.Consumption(
            resultId,
            gridArea,
            settlementType,
            "KWH",
            "PT1H",
            period,
            ExtractPoints(resultDto!.TimeSeriesPoints));
    }

    private static IReadOnlyList<Point> ExtractPoints(TimeSeriesPointDto[] timeSeriesPoints)
    {
        var points = new List<Point>();
        for (int i = 0; i < timeSeriesPoints.Length; i++)
        {
            points.Add(new Point(i + 1, timeSeriesPoints[i].Quantity, timeSeriesPoints[i].Quality, NodaTime.Instant.FromDateTimeOffset(timeSeriesPoints[i].Time).ToString()));
        }

        return points.AsReadOnly();
    }

    private static MeteringPointType ParseMeteringPointType(ProcessStepMeteringPointType type)
    {
        return type switch
        {
            ProcessStepMeteringPointType.Production => MeteringPointType.Production,
            _ => MeteringPointType.Consumption,
        };
    }

    private record ProcessStepResultDto(
        ProcessStepMeteringPointType ProcessStepMeteringPointType,
        decimal Sum,
        decimal Min,
        decimal Max,
        TimeSeriesPointDto[] TimeSeriesPoints);

    private record TimeSeriesPointDto(
        DateTimeOffset Time,
        decimal Quantity,
        string Quality);
}
