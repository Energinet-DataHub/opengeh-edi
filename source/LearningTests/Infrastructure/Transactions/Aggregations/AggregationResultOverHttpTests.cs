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

using System.Reflection;
using Infrastructure.Configuration.Serialization;
using Infrastructure.Transactions;
using Infrastructure.Transactions.Aggregations;
using Microsoft.Extensions.Configuration;

namespace LearningTests.Infrastructure.Transactions.Aggregations;

public class AggregationResultOverHttpTests
{
    private readonly Guid _batchId = Guid.Parse("006d5d41-fd58-4510-9621-122c83044b43");
    private readonly string _gridArea = "543";
    private readonly IConfigurationRoot _configuration;
    private readonly AggregationResultMapper _mapper;
    private readonly ISerializer _serializer;

    public AggregationResultOverHttpTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), false, false)
            .Build();

        _serializer = new Serializer();
        _mapper = new AggregationResultMapper(_serializer);
    }

    [Fact]
    public async Task Can_retrieve_result()
    {
        using var httpClient = new HttpClient();
        var service = new AggregationResultsOverHttp(new HttpClientAdapter(httpClient), new Uri(_configuration["ServiceUri"]!), _mapper, _serializer);

        var result = await service.GetResultAsync(_batchId, _gridArea).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Fetch_list_of_energy_suppliers_for_which_a_flex_consumption_result_is_available()
    {
        using var httpClient = new HttpClient();
        var service = new AggregationResultsOverHttp(new HttpClientAdapter(httpClient), new Uri(_configuration["ServiceUri"]!), _mapper, _serializer);

        var energySuppliers = await service.EnergySuppliersWithHourlyConsumptionResultAsync(_batchId, _gridArea)
            .ConfigureAwait(false);

        Assert.NotEmpty(energySuppliers);
    }
}
