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

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LearningTests;

public class WholesaleProcessResultsApiTests
{
    private readonly IConfigurationRoot _configuration;
    private readonly Guid _batchId = Guid.Parse("006d5d41-fd58-4510-9621-122c83044b43");
    private readonly string _gridArea = "543";

    public WholesaleProcessResultsApiTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), false, false)
            .Build();
    }

    [Fact]
    public async Task Fetch_aggregated_production_per_grid_area()
    {
        using var httpClient = new HttpClient();

        var request = new ProcessStepResultRequestDto(_batchId, _gridArea, ProcessStepType.AggregateProductionPerGridArea);
        var response = await httpClient.PostAsJsonAsync(ServiceUriForVersion("2.1"), request).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<ProcessStepResultDto>().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Fetch_list_of_actor_for_which_a_result_is_generated()
    {
        using var httpClient = new HttpClient();

        var request = new ProcessStepActorsRequest(Guid.Parse("006d5d41-fd58-4510-9621-122c83044b43"), _gridArea, TimeSeriesType.NonProfiledConsumption, MarketRole.EnergySupplier);
        var response = await httpClient.PostAsJsonAsync(ServiceUriForVersion("2.3"), request).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<List<WholesaleActorDto>>().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Fetch_hourly_consumption_per_energy_supplier()
    {
        using var httpClient = new HttpClient();

        var energySuppliersListResponse = await httpClient.PostAsJsonAsync(
            ServiceUriForVersion("2.3"),
            new ProcessStepActorsRequest(_batchId, _gridArea, TimeSeriesType.NonProfiledConsumption, MarketRole.EnergySupplier)).ConfigureAwait(false);
        energySuppliersListResponse.EnsureSuccessStatusCode();

        var energySuppliers = await energySuppliersListResponse.Content.ReadFromJsonAsync<List<WholesaleActorDto>>().ConfigureAwait(false);

        var requestAggregationResult = await httpClient.PostAsJsonAsync(
            ServiceUriForVersion("2.2"),
            new ProcessStepResultRequestDtoV2(
                _batchId,
                _gridArea,
                TimeSeriesType.NonProfiledConsumption,
                energySuppliers![0].Gln)).ConfigureAwait(false);

        requestAggregationResult.EnsureSuccessStatusCode();

        var timeSeries = await requestAggregationResult.Content.ReadAsStringAsync().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, requestAggregationResult.StatusCode);
        Assert.NotNull(timeSeries);
    }

    #pragma warning disable
    public sealed record ProcessStepActorsRequest(Guid BatchId, string GridAreaCode, TimeSeriesType Type, MarketRole MarketRole);

    public enum MarketRole
    {
        EnergySupplier = 0,
    }

    public enum TimeSeriesType
    {
        NonProfiledConsumption = 1,
        FlexConsumption = 2,
        Production = 3,
    }

    public sealed record WholesaleActorDto(string Gln);

    public sealed record ProcessStepResultRequestDtoV2(Guid BatchId, string GridAreaCode, TimeSeriesType TimeSeriesType, string Gln);

    public record ProcessStepResultRequestDto(Guid BatchId, string GridAreaCode, ProcessStepType ProcessStepResult);

    public enum ProcessStepType
    {
        AggregateProductionPerGridArea = 25,
    }

    public record ProcessStepResultDto(
        ProcessStepMeteringPointType ProcessStepMeteringPointType,
        decimal Sum,
        decimal Min,
        decimal Max,
        TimeSeriesPointDto[] TimeSeriesPoints);

    public enum ProcessStepMeteringPointType
    {
        Production = 0,
    }

    public record TimeSeriesPointDto(
        DateTimeOffset Time,
        decimal Quantity);

    private Uri ServiceUriForVersion(string version)
    {
        return new Uri($"{_configuration["ServiceUri"]!}/v{version}/processstepresult");
    }
}
