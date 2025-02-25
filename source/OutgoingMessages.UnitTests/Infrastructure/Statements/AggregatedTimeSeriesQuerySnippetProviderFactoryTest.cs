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
using Energinet.DataHub.EDI.BuildingBlocks.Tests;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Statements;

public class AggregatedTimeSeriesQuerySnippetProviderFactoryTest
{
    [Fact]
    public void Given_AggregatedDatabricksContracts_When_Resolved_Then_ResolvesToExpectedDictionary()
    {
        var expectedContracts = Assembly.GetAssembly(typeof(IAggregatedTimeSeriesDatabricksContract))!
            .GetTypes()
            .Where(
                t => typeof(IAggregatedTimeSeriesDatabricksContract).IsAssignableFrom(t)
                     && t is { IsInterface: false, IsAbstract: false })
            .Select(t => (IAggregatedTimeSeriesDatabricksContract)Activator.CreateInstance(t)!)
            .ToList();

        var sut = new AggregatedTimeSeriesQuerySnippetProviderFactory(expectedContracts);

        var fieldInfo =
            typeof(AggregatedTimeSeriesQuerySnippetProviderFactory).GetField(
                "_databricksContracts",
                BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Field '_databricksContracts' not found.");

        var actualContracts = (Dictionary<AggregationLevel, IAggregatedTimeSeriesDatabricksContract>)fieldInfo.GetValue(sut)!;

        expectedContracts.Should().OnlyHaveUniqueItems(ec => ec.GetAggregationLevel());

        actualContracts.Select(ac => ac.Key)
            .Should()
            .BeEquivalentTo(expectedContracts.Select(ec => ec.GetAggregationLevel()));

        actualContracts.Select(ac => ac.Value)
            .Should()
            .BeEquivalentTo(expectedContracts);
    }

    [Fact]
    [ExcludeFromNameConventionCheck]
    public void Provider_keys_are_limited_to_delta_table_aggregation_levels()
    {
        var contracts = Assembly.GetAssembly(typeof(IAggregatedTimeSeriesDatabricksContract))!
            .GetTypes()
            .Where(
                t => typeof(IAggregatedTimeSeriesDatabricksContract).IsAssignableFrom(t)
                     && t is { IsInterface: false, IsAbstract: false })
            .Select(t => (IAggregatedTimeSeriesDatabricksContract)Activator.CreateInstance(t)!)
            .ToList();

        var expectedNameSequence = new List<AggregationLevel>()
        {
            AggregationLevel.GridArea,
            AggregationLevel.BalanceResponsibleAndGridArea,
            AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea,
        };

        Assert.Equal(contracts.Select(contract => contract.GetAggregationLevel()).Order(), expectedNameSequence);
    }
}
