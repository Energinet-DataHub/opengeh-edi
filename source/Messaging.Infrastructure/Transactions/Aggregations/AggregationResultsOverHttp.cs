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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Domain.Transactions.Aggregations;

namespace Messaging.Infrastructure.Transactions.Aggregations;

public class AggregationResultsOverHttp : IAggregationResults
{
    private readonly HttpClient _httpClient;
    private readonly Uri _serviceEndpoint;

    public AggregationResultsOverHttp(HttpClient httpClient, Uri serviceEndpoint)
    {
        _httpClient = httpClient;
        _serviceEndpoint = serviceEndpoint;
    }

    private enum ProcessStepMeteringPointType
    {
        Production = 0,
    }

    private enum ProcessStepType
    {
        AggregateProductionPerGridArea = 25,
    }

    public async Task<AggregationResult> GetResultAsync(Guid resultId, string gridArea)
    {
        var request = new ProcessStepResultRequestDto(resultId, gridArea, ProcessStepType.AggregateProductionPerGridArea);
        var response = await _httpClient.PostAsJsonAsync(_serviceEndpoint, request).ConfigureAwait(false);
        var resultDto = await response.Content.ReadFromJsonAsync<ProcessStepResultDto>().ConfigureAwait(false);

        return new AggregationResult(
            resultId,
            ExtractPoints(resultDto!.TimeSeriesPoints),
            gridArea,
            "E18",
            "KWH",
            "PT1H");
    }

    private static IReadOnlyList<Point> ExtractPoints(TimeSeriesPointDto[] timeSeriesPoints)
    {
        var points = new List<Point>();
        for (int i = 0; i < timeSeriesPoints.Length; i++)
        {
            points.Add(new Point(i, timeSeriesPoints[i].Quantity, "quality", NodaTime.Instant.FromDateTimeOffset(timeSeriesPoints[i].Time).ToString()));
        }

        return points.AsReadOnly();
    }

    private record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, ProcessStepType ProcessStepResult);

    private record ProcessStepResultDto(
        ProcessStepMeteringPointType ProcessStepMeteringPointType,
        decimal Sum,
        decimal Min,
        decimal Max,
        TimeSeriesPointDto[] TimeSeriesPoints);

    private record TimeSeriesPointDto(
        DateTimeOffset Time,
        decimal Quantity);
}
