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
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Transactions.Aggregations;
using Microsoft.Extensions.Configuration;

namespace LearningTests.Infrastructure.Transactions.Aggregations;

public class AggregationResultOverHttpTests
{
    private readonly IConfigurationRoot _configuration;
    private readonly AggregationResultMapper _mapper;

    public AggregationResultOverHttpTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), false, false)
            .Build();

        _mapper = new AggregationResultMapper(new Serializer());
    }

    [Fact]
    public async Task Service_is_unavailable()
    {
        using var httpClient = new HttpClient();
        var service = new AggregationResultsOverHttp(httpClient, new Uri($"{_configuration["ServiceUri"]!}/WrongUri"), _mapper);

        await Assert.ThrowsAnyAsync<Exception>(() => service.GetResultAsync(Guid.Parse(_configuration["BatchId"]!), _configuration["GridArea"]!)).ConfigureAwait(false);
    }

    [Fact]
    public async Task Can_retrieve_result()
    {
        using var httpClient = new HttpClient();
        var service = new AggregationResultsOverHttp(httpClient, new Uri(_configuration["ServiceUri"]!), _mapper);

        var result = await service.GetResultAsync(Guid.Parse(_configuration["BatchId"]!), _configuration["GridArea"]!).ConfigureAwait(false);

        Assert.NotNull(result);
    }
}
