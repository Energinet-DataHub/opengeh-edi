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
using Microsoft.Extensions.Configuration;

namespace LearningTests;

public class WholesaleProcessResultsApiTests
{
    private readonly IConfigurationRoot _configuration;

    public WholesaleProcessResultsApiTests()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, false)
            .Build();
    }

    [Fact]
    public async Task Fetch_aggregated_production_per_grid_area()
    {
        using var httpClient = new HttpClient();

        var request = new ProcessStepResultRequestDto(Guid.Parse(_configuration["BatchId"]!), _configuration["GridArea"]!, ProcessStepType.AggregateProductionPerGridArea);
        var response = await httpClient.PostAsJsonAsync(new Uri(_configuration["ServiceUri"]!), request).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<ProcessStepResultDto>().ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
    }

    #pragma warning disable
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
}
