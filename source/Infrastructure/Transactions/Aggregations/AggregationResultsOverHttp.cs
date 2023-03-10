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
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Polly;
using Period = Domain.Transactions.Aggregations.Period;

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

    public async Task<AggregationResult?> ProductionResultForAsync(Guid resultId, string gridArea, Domain.Transactions.Aggregations.Period period)
    {
        var response = await CallAsync("2.1", new WholeSaleContracts.ProcessStepResultRequestDto(resultId, gridArea, WholeSaleContracts.ProcessStepType.AggregateProductionPerGridArea)).ConfigureAwait(false);

        return await MapFromAsync(resultId, gridArea, period, response).ConfigureAwait(false);
    }

    public async Task<ReadOnlyCollection<ActorNumber>> EnergySuppliersWithHourlyConsumptionResultAsync(Guid resultId, string gridArea)
    {
        var response = await CallAsync("2.3", new WholeSaleContracts.ProcessStepActorsRequest(
            resultId,
            gridArea,
            WholeSaleContracts.TimeSeriesType.NonProfiledConsumption,
            WholeSaleContracts.MarketRole.EnergySupplier))
            .ConfigureAwait(false);

        var actorNumbers = await response.Content.ReadFromJsonAsync<List<WholeSaleContracts.WholesaleActorDto>>().ConfigureAwait(false);
        return actorNumbers?
            .Where(actorNumber => !string.IsNullOrEmpty(actorNumber.Gln))
            .Select(actorNumber => ActorNumber.Create(actorNumber.Gln))
            .ToList()
            .AsReadOnly()!;
    }

    public async Task<AggregationResult> NonProfiledConsumptionForAsync(Guid resultId, string gridArea, ActorNumber energySupplierNumber, Domain.Transactions.Aggregations.Period period)
    {
        ArgumentNullException.ThrowIfNull(energySupplierNumber);

        var response = await CallAsync(
                "2.2",
                new WholeSaleContracts.ProcessStepResultRequestDtoV2(
                    resultId,
                    gridArea,
                    WholeSaleContracts.TimeSeriesType.NonProfiledConsumption,
                    energySupplierNumber.Value))
            .ConfigureAwait(false);

        return await _aggregationResultMapper.MapToConsumptionResultAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false), resultId, gridArea, period, SettlementType.NonProfiled).ConfigureAwait(false);
    }

    public Task<ReadOnlyCollection<Result>> NonProfiledConsumptionForAsync(Guid resultId, GridArea gridArea, MarketRole roleOfReceiver, Period period)
    {
        return Task.FromResult(new List<Result>().AsReadOnly());
    }

    public async Task<AggregationResult> TotalNonProfiledConsumptionForBalanceResponsibleAsync(
        Guid resultsId,
        ProcessType aggregationProcess,
        GridArea gridArea,
        Domain.Transactions.Aggregations.Period period,
        ActorNumber balanceResponsible)
    {
        ArgumentNullException.ThrowIfNull(balanceResponsible);
        ArgumentNullException.ThrowIfNull(gridArea);
        var requestUri = new Uri(
            _serviceEndpoint, $"v3/batches/{resultsId}/processes/{gridArea.Code}/time-series-types/1?balanceResponsiblePartyGln={balanceResponsible.Value}");
        var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await _aggregationResultMapper.MapToConsumptionResultAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false),  resultsId, gridArea.Code, period, SettlementType.NonProfiled).ConfigureAwait(false);
    }

    public async Task<ReadOnlyCollection<ActorNumber>> BalanceResponsiblesWithTotalNonProfiledConsumptionAsync(Guid resultsId, GridArea gridArea)
    {
        ArgumentNullException.ThrowIfNull(gridArea);
        var requestUri = new Uri(_serviceEndpoint, $"v3/batches/{resultsId}/processes/{gridArea.Code}/time-series-types/1/balance-responsible-parties");
        var response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
        var actorNumbers = await response.Content.ReadFromJsonAsync<List<WholeSaleContracts.WholesaleActorDto>>().ConfigureAwait(false);
        return actorNumbers?
            .Where(actorNumber => !string.IsNullOrEmpty(actorNumber.Gln))
            .Select(actorNumber => ActorNumber.Create(actorNumber.Gln))
            .ToList()
            .AsReadOnly()!;
    }

    private async Task<HttpResponseMessage> CallAsync<TRequest>(string apiVersion, TRequest request)
    {
        var executionPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(3),
            });

        var response = await executionPolicy.ExecuteAsync(() => _httpClient.PostAsync(
            ServiceUriFor(apiVersion),
            CreateContentFrom(request))).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        return response;
    }

    private Uri ServiceUriFor(string apiVersion)
    {
        return new Uri(_serviceEndpoint, $"v{apiVersion}/processstepresult");
    }

    private StringContent CreateContentFrom<T>(T request)
    {
        return new StringContent(_serializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json);
    }

    private async Task<AggregationResult> MapFromAsync(Guid resultId, string gridArea, Domain.Transactions.Aggregations.Period period, HttpResponseMessage response)
    {
        return await _aggregationResultMapper.MapProductionResultAsync(
            await response.Content.ReadAsStreamAsync().ConfigureAwait(false), resultId, gridArea, period).ConfigureAwait(false);
    }
}
