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
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Polly;

namespace Infrastructure.Transactions.Aggregations;

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

    private enum TimeSeriesType
    {
        NonProfiledConsumption = 1,
        FlexConsumption = 2,
        Production = 3,
    }

    private enum MarketRole
    {
        EnergySupplier = 0,
    }

    public async Task<AggregationResult> GetResultAsync(Guid resultId, string gridArea)
    {
        var response = await CallAsync("2.1", new ProcessStepResultRequestDto(resultId, gridArea, ProcessStepType.AggregateProductionPerGridArea)).ConfigureAwait(false);

        return await _aggregationResultMapper.MapFromAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false), resultId, gridArea).ConfigureAwait(false);
    }

    public async Task<ReadOnlyCollection<ActorNumber>> EnergySuppliersWithHourlyConsumptionResultAsync(Guid resultId, string gridArea)
    {
        var response = await CallAsync("2.3", new ProcessStepActorsRequest(
            resultId,
            gridArea,
            TimeSeriesType.NonProfiledConsumption,
            MarketRole.EnergySupplier))
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var actorNumbers = await response.Content.ReadFromJsonAsync<List<WholesaleActorDto>>().ConfigureAwait(false);
        return actorNumbers?
            .Where(actorNumber => !string.IsNullOrEmpty(actorNumber.Gln))
            .Select(actorNumber => ActorNumber.Create(actorNumber.Gln))
            .ToList()
            .AsReadOnly()!;
    }

    public Task<AggregationResult> HourlyConsumptionForAsync(Guid resultId, string gridArea, ActorNumber energySupplierNumber)
    {
        throw new NotImplementedException();
    }

    private Task<HttpResponseMessage> CallAsync<TRequest>(string apiVersion, TRequest request)
    {
        var executionPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
            });

        return executionPolicy.ExecuteAsync(() => _httpClient.PostAsync(
            ServiceUriFor(apiVersion),
            CreateContentFrom(request)));
    }

    private Uri ServiceUriFor(string apiVersion)
    {
        return new Uri(_serviceEndpoint, $"v{apiVersion}/processstepresult");
    }

    private StringContent CreateContentFrom<T>(T request)
    {
        return new StringContent(_serializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);
    }

    private record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, ProcessStepType ProcessStepResult);

    private record ProcessStepActorsRequest(Guid BatchId, string GridAreaCode, TimeSeriesType Type, MarketRole MarketRole);

    private record WholesaleActorDto(string Gln);
}
