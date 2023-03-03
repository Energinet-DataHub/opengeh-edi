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
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Infrastructure.Transactions;
using Infrastructure.Transactions.Aggregations;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Period = Domain.Transactions.Aggregations.Period;

namespace LearningTests.Infrastructure.Transactions.Aggregations;

public class AggregationResultOverHttpTests : IDisposable
{
    private readonly Guid _batchId = Guid.Parse("006d5d41-fd58-4510-9621-122c83044b43");
    private readonly string _gridArea = "543";
    private readonly IConfigurationRoot _configuration;
    private readonly HttpClient _httpClient;
    private readonly AggregationResultsOverHttp _service;

    public AggregationResultOverHttpTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), false, false)
            .Build();

        ISerializer serializer = new Serializer();
        _httpClient = new HttpClient();
        _service = new AggregationResultsOverHttp(new HttpClientAdapter(_httpClient), new Uri(_configuration["ServiceUri"]!), new AggregationResultMapper(serializer), serializer);
    }

    ~AggregationResultOverHttpTests()
    {
        Dispose(false);
    }

    [Fact]
    public async Task Can_retrieve_result()
    {
        var result = await _service.ProductionResultForAsync(_batchId, _gridArea, new Period(NodaTime.SystemClock.Instance.GetCurrentInstant(), NodaTime.SystemClock.Instance.GetCurrentInstant())).ConfigureAwait(false);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Fetch_list_of_energy_suppliers_for_which_a_non_profiled_consumption_result_is_available()
    {
        var energySuppliers = await _service.EnergySuppliersWithHourlyConsumptionResultAsync(_batchId, _gridArea)
            .ConfigureAwait(false);

        Assert.NotEmpty(energySuppliers);
    }

    [Fact]
    public async Task Fetch_list_of_balance_responsibles_for_which_a_total_non_profiled_consumption_result_is_available()
    {
        var batchId = Guid.Parse("607B9862-9273-4A06-9382-E7194BF57A1B");
        var balanceResponsibles =
            await _service.BalanceResponsiblesWithTotalNonProfiledConsumptionAsync(batchId,  GridArea.Create("543")).ConfigureAwait(false);

        Assert.NotEmpty(balanceResponsibles);
    }

    [Fact]
    public async Task Fetch_non_profiled_consumption_aggregation_result_for_a_single_energy_supplier()
    {
        var energySuppliers = await _service.EnergySuppliersWithHourlyConsumptionResultAsync(_batchId, _gridArea)
            .ConfigureAwait(false);

        var aggregationResult = await _service.NonProfiledConsumptionForAsync(_batchId, _gridArea, energySuppliers[0], new Period(NodaTime.SystemClock.Instance.GetCurrentInstant(), NodaTime.SystemClock.Instance.GetCurrentInstant()))
            .ConfigureAwait(false);

        Assert.NotNull(aggregationResult);
    }

    [Fact]
    public async Task Fetch_total_non_profiled_consumption_result_for_balance_responsible()
    {
        var batchId = Guid.Parse("5C9C5BEB-BBF7-45F0-B435-2FFA1FD56C1C");
        var balanceResponsibles =
            await _service.BalanceResponsiblesWithTotalNonProfiledConsumptionAsync(batchId,  GridArea.Create("543")).ConfigureAwait(false);

        var aggregationResult = await _service.TotalNonProfiledConsumptionForBalanceResponsibleAsync(
                batchId,
                ProcessType.BalanceFixing,
                GridArea.Create(_gridArea),
                new Period(NodaTime.SystemClock.Instance.GetCurrentInstant(), NodaTime.SystemClock.Instance.GetCurrentInstant()),
                balanceResponsibles[0])
            .ConfigureAwait(false);

        Assert.NotNull(aggregationResult);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }
}
