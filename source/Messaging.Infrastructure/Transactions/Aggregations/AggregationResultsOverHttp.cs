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
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Configuration.Serialization;

namespace Messaging.Infrastructure.Transactions.Aggregations;

public class AggregationResultsOverHttp : IAggregationResults
{
    private readonly IHttpClientAdapter _httpClient;
    private readonly Uri _serviceEndpoint;
    private readonly AggregationResultMapper _aggregationResultMapper;
    private readonly ISerializer _serializer;

    public AggregationResultsOverHttp(IHttpClientAdapter httpClient, Uri serviceEndpoint, AggregationResultMapper aggregationResultMapper, ISerializer serializer)
    {
        _httpClient = httpClient;
        _serviceEndpoint = serviceEndpoint;
        _aggregationResultMapper = aggregationResultMapper;
        _serializer = serializer;
    }

    private enum ProcessStepType
    {
        AggregateProductionPerGridArea = 25,
    }

    public async Task<AggregationResult> GetResultAsync(Guid resultId, string gridArea)
    {
        using var httpContent = CreateRequest(resultId, gridArea);
        var response = await _httpClient.PostAsync(_serviceEndpoint, httpContent).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await _aggregationResultMapper.MapFromAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false), resultId, gridArea).ConfigureAwait(false);
    }

    private StringContent CreateRequest(Guid resultId, string gridArea)
    {
        var request = new ProcessStepResultRequestDto(resultId, gridArea, ProcessStepType.AggregateProductionPerGridArea);
        var httpContent =
            new StringContent(_serializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);
        return httpContent;
    }

    private record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, ProcessStepType ProcessStepResult);
}
