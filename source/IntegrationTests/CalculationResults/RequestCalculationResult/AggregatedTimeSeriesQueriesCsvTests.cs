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

using AutoFixture;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Formats;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.CalculationResults.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using BusinessReason = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason;

namespace Energinet.DataHub.EDI.IntegrationTests.CalculationResults.RequestCalculationResult;

public class AggregatedTimeSeriesQueriesCsvTests
{
    private const string EnergySupplierOne = "5790002617263";
    private const string EnergySupplierTwo = "5790000701414";
    private const string EnergySupplierThree = "5790001687137";

    private const string BalanceResponsibleOne = "5790000701414";
    private const string BalanceResponsibleTwo = "5790001964597";

    /// <summary>
    /// Tests that creates data once when the fixture is initialized, and shares the data between tests.
    /// </summary>
    public class AggregatedTimeSeriesQueriesCsvTestsWithSharedData
        : TestBase<AggregatedTimeSeriesQueries>,
            IClassFixture<MigrationsFreeDatabricksSqlStatementApiFixture>,
            IAsyncLifetime
    {
        private readonly MigrationsFreeDatabricksSqlStatementApiFixture _fixture;
        private readonly ITestOutputHelper _testOutputHelper;

        public AggregatedTimeSeriesQueriesCsvTestsWithSharedData(
            MigrationsFreeDatabricksSqlStatementApiFixture fixture,
            ITestOutputHelper testOutputHelper)
        {
            Fixture.Inject(fixture.DatabricksSchemaManager.DeltaTableOptions);
            Fixture.Inject(fixture.GetDatabricksExecutor());
            Fixture.Inject(new AggregatedTimeSeriesQuerySnippetProviderFactory([
                new EnergyPerGaAggregatedTimeSeriesDatabricksContract(),
                new EnergyPerBrpGaAggregatedTimeSeriesDatabricksContract(),
                new EnergyPerEsBrpGaAggregatedTimeSeriesDatabricksContract()]));

            _fixture = fixture;
            _testOutputHelper = testOutputHelper;
        }

        public async Task InitializeAsync()
        {
            if (!_fixture.DataIsInitialized)
            {
                await ClearAndAddDatabricksDataAsync(_fixture.DatabricksSchemaManager, _testOutputHelper);
                _fixture.DataIsInitialized = true;
            }
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task Given_EnergySupplierAcrossGridAreas_When_Queried_Then_RelevantDataFromRelevantGridAreasReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: [],
                EnergySupplierId: EnergySupplierThree,
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.BalanceFixing,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Should().HaveCount(18);
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 16),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_EnergySupplierAndGridArea_When_Queried_Then_RelevantDataFromSpecifiedGridAreaReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.Flex,
                GridAreaCodes: ["804"],
                EnergySupplierId: EnergySupplierOne,
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.BalanceFixing,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_EnergySupplierAndBalanceResponsibleAndGridArea_When_Queried_Then_DataFilteredCorrectlyReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.Flex,
                GridAreaCodes: ["804"],
                EnergySupplierId: EnergySupplierOne,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.BalanceFixing,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_BalanceResponsibleAndGridArea_When_Queried_Then_RelevantDataFromGridAreaReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.Flex,
                GridAreaCodes: ["804"],
                EnergySupplierId: null,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.BalanceFixing,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Should().HaveCount(6);
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .OrderBy(t => t.PeriodStart)
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 6),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("804", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_GridArea_When_Queried_Then_GridOperatorDataForGridAreaReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Production,
                SettlementMethod: null,
                GridAreaCodes: ["804"],
                EnergySupplierId: null,
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.BalanceFixing,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Should().HaveCount(6);
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Production, (SettlementMethod?)null, Instant.FromUtc(2022, 1, 5, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 6),
                    ("804", MeteringPointType.Production, null, Instant.FromUtc(2022, 1, 7, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 6),
                    ("804", MeteringPointType.Production, null, Instant.FromUtc(2022, 1, 1, 23, 0), Instant.FromUtc(2022, 1, 3, 23, 0), 8),
                    ("804", MeteringPointType.Production, null, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 7, 23, 0), 8),
                    ("804", MeteringPointType.Production, null, Instant.FromUtc(2022, 1, 3, 23, 0), Instant.FromUtc(2022, 1, 5, 23, 0), 8),
                    ("804", MeteringPointType.Production, null, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 1, 23, 0), 7),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_FullQueryParametersForAggregation_When_Queried_Then_DataFromNewestVersionsReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: ["543"],
                EnergySupplierId: EnergySupplierThree,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.PreliminaryAggregation,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 2, 23, 0), 9),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 2, 23, 0), Instant.FromUtc(2022, 1, 4, 23, 0), 10),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 4, 23, 0), Instant.FromUtc(2022, 1, 6, 23, 0), 11),
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 6, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), 9),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_EnergySupplierAndBalanceResponsibleWithLatestCorrection_When_Queried_Then_DataFromNewestCorrectionsReturned()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: [],
                EnergySupplierId: EnergySupplierThree,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.Correction,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.BusinessReason,
                    ats.SettlementVersion, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("543", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), BusinessReason.Correction, SettlementVersion.SecondCorrection, 4),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), BusinessReason.Correction, SettlementVersion.ThirdCorrection, 2),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.HaveCount((int)ats.PeriodEnd.Minus(ats.PeriodStart).TotalHours)
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_NoEnergySupplierAndBalanceResponsibleAndGridArea_When_Queried_Then_IdenticalToRequestsForEachGridAreaIndividually()
        {
            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: [],
                EnergySupplierId: null,
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.PreliminaryAggregation,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            var eachGridAreaIndividually = new List<AggregatedTimeSeries>();
            foreach (var parametersForGridArea in new[] { "543", "584", "804", }
                         .Select(gridArea => parameters with { GridAreaCodes = [gridArea] }))
            {
                eachGridAreaIndividually.AddRange(await Sut.GetAsync(parametersForGridArea).ToListAsync());
            }

            actual.Should().NotBeEmpty().And.BeEquivalentTo(eachGridAreaIndividually);
        }
    }

    /// <summary>
    /// Tests that each clear/create their needed databricks data
    /// </summary>
    public class AggregatedTimeSeriesQueriesCsvTestsWithIndividualData
        : TestBase<AggregatedTimeSeriesQueries>, IClassFixture<MigrationsFreeDatabricksSqlStatementApiFixture>
    {
        private readonly MigrationsFreeDatabricksSqlStatementApiFixture _fixture;
        private readonly ITestOutputHelper _testOutputHelper;

        public AggregatedTimeSeriesQueriesCsvTestsWithIndividualData(
            MigrationsFreeDatabricksSqlStatementApiFixture fixture,
            ITestOutputHelper testOutputHelper)
        {
            Fixture.Inject(fixture.DatabricksSchemaManager.DeltaTableOptions);
            Fixture.Inject(fixture.GetDatabricksExecutor());
            Fixture.Inject(new AggregatedTimeSeriesQuerySnippetProviderFactory([
                new EnergyPerGaAggregatedTimeSeriesDatabricksContract(),
                new EnergyPerBrpGaAggregatedTimeSeriesDatabricksContract(),
                new EnergyPerEsBrpGaAggregatedTimeSeriesDatabricksContract()]));
            _fixture = fixture;
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Given_EnergySupplierOnlyHaveDataForHalfOfThePeriod_When_Queried_Then_DataReturnedWithModifiedPeriod()
        {
            /*
             Business case example:
             When a new Energy Supplier is being made responsible for a metering point in the middle of the month,
             and they do not yet have a metering point in the grid area from the beginning of the month.
             The result is that the Energy Supplier will only have results for the last half of the month.
            */
            await ClearAndAddDatabricksDataAsync(_fixture.DatabricksSchemaManager, _testOutputHelper);
            await RemoveDataForEnergySupplierInTimespan(
                _fixture,
                _testOutputHelper,
                "5790002617263",
                Instant.FromUtc(2022, 1, 4, 0, 0),
                null);

            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: [],
                EnergySupplierId: "5790002617263",
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.Correction,
                SettlementVersion: SettlementVersion.SecondCorrection,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.BusinessReason,
                    ats.SettlementVersion, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 4, 1, 0), Instant.FromUtc(2022, 1, 8, 23, 0), BusinessReason.Correction, SettlementVersion.SecondCorrection, 3),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should()
                    .AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeGreaterOrEqualTo(ats.PeriodStart))
                    .And.AllSatisfy(etsp => etsp.Time.ToInstant().Should().BeLessThan(ats.PeriodEnd))
                    .And.AllSatisfy(etsp =>
                    {
                        etsp.Time.Minute.Should().Be(0);
                        etsp.Time.Second.Should().Be(0);
                    })
                    .And.OnlyHaveUniqueItems(etsp => etsp.Time);
            });
        }

        [Fact]
        public async Task Given_EnergySupplierWithAHoleInData_When_Queried_Then_DataReturnedInTwoChunkWithoutAHole()
        {
            await ClearAndAddDatabricksDataAsync(_fixture.DatabricksSchemaManager, _testOutputHelper);
            await RemoveDataForEnergySupplierInTimespan(
                _fixture,
                _testOutputHelper,
                "5790002617263",
                Instant.FromUtc(2022, 1, 5, 0, 0),
                Instant.FromUtc(2022, 1, 3, 0, 0));

            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.NonProfiled,
                GridAreaCodes: [],
                EnergySupplierId: "5790002617263",
                BalanceResponsibleId: null,
                BusinessReason: BusinessReason.Correction,
                SettlementVersion: SettlementVersion.ThirdCorrection,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.BusinessReason,
                    ats.SettlementVersion, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 3, 1, 0), BusinessReason.Correction, SettlementVersion.ThirdCorrection, 2),
                    ("804", MeteringPointType.Consumption, SettlementMethod.NonProfiled, Instant.FromUtc(2022, 1, 5, 1, 0), Instant.FromUtc(2022, 1, 8, 23, 0), BusinessReason.Correction, SettlementVersion.ThirdCorrection, 2),
                ]);

            actual.Should().AllSatisfy(ats =>
            {
                ats.TimeSeriesPoints.Should().AllSatisfy(etsp =>
                {
                    ((object)etsp.Time).Should().Match<DateTimeOffset>(time =>
                        time <= new DateTimeOffset(2022, 1, 3, 0, 0, 0, TimeSpan.Zero)
                        || time > new DateTimeOffset(2022, 1, 5, 0, 0, 0, TimeSpan.Zero));
                });
            });
        }

        [Fact]
        public async Task Given_BalanceResponsibleWithLatestCorrectionButNoCorrectionData_When_Queried_Then_NoDataReturned()
        {
            await ClearAndAddDatabricksDataAsync(_fixture.DatabricksSchemaManager, _testOutputHelper);
            await RemoveDataForCorrections(_fixture, _testOutputHelper, []);

            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Production,
                SettlementMethod: null,
                GridAreaCodes: [],
                EnergySupplierId: null,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.Correction,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Should().BeEmpty();
        }

        [Fact]
        public async Task Given_EnergySupplierAndBalanceResponsibleWithLatestCorrectionButOnlyOneGridAreaWithCorrectionData_When_Queried_Then_DataReturnedForGridArea()
        {
            await ClearAndAddDatabricksDataAsync(_fixture.DatabricksSchemaManager, _testOutputHelper);
            await RemoveDataForCorrections(_fixture, _testOutputHelper, ["804", "543"]);

            var totalPeriod = new OutgoingMessages.Interfaces.Models.CalculationResults.Period(
                Instant.FromUtc(2021, 12, 31, 23, 0),
                Instant.FromUtc(2022, 1, 8, 23, 0));

            var parameters = new AggregatedTimeSeriesQueryParameters(
                MeteringPointType: MeteringPointType.Consumption,
                SettlementMethod: SettlementMethod.Flex,
                GridAreaCodes: [],
                EnergySupplierId: EnergySupplierTwo,
                BalanceResponsibleId: BalanceResponsibleOne,
                BusinessReason: BusinessReason.Correction,
                SettlementVersion: null,
                Period: totalPeriod);

            // Act
            var actual = await Sut.GetAsync(parameters).ToListAsync();

            using var assertionScope = new AssertionScope();
            actual.Select(ats => (ats.GridArea, ats.MeteringPointType, ats.SettlementMethod, ats.PeriodStart, ats.PeriodEnd, ats.BusinessReason, ats.SettlementVersion, ats.Version))
                .Should()
                .BeEquivalentTo([
                    ("584", MeteringPointType.Consumption, SettlementMethod.Flex, Instant.FromUtc(2021, 12, 31, 23, 0), Instant.FromUtc(2022, 1, 8, 23, 0), BusinessReason.Correction, SettlementVersion.SecondCorrection, 3),
                ]);
        }
    }

#pragma warning disable SA1201
    private static async Task ClearAndAddDatabricksDataAsync(
        MigrationsFreeDatabricksSchemaManager databricksSchemaManager,
        ITestOutputHelper testOutputHelper)
    {
        using (new PerformanceLogger(testOutputHelper, "ClearAndAddDatabricksDataAsync"))
        {
            using (new PerformanceLogger(testOutputHelper, "Drop databricks schema"))
            {
                await databricksSchemaManager.DropSchemaAsync();
            }

            using (new PerformanceLogger(testOutputHelper, "Create databricks schema"))
            {
                await databricksSchemaManager.CreateSchemaAsync();
            }

            const string view1 = "wholesale_calculation_results.energy_per_ga_v1.csv";
            var view1File = Path.Combine("CalculationResults", "TestData", view1);

            const string view2 = "wholesale_calculation_results.energy_per_brp_ga_v1.csv";
            var view2File = Path.Combine("CalculationResults", "TestData", view2);

            const string view3 = "wholesale_calculation_results.energy_per_es_brp_ga_v1.csv";
            var view3File = Path.Combine("CalculationResults", "TestData", view3);

            using (new PerformanceLogger(testOutputHelper, "Insert ENERGY_PER_GA in databricks"))
            {
                await databricksSchemaManager.InsertFromCsvFileAsync(
                    databricksSchemaManager.DeltaTableOptions.Value.ENERGY_V1_VIEW_NAME,
                    EnergyPerGaViewSchemaDefinition.SchemaDefinition,
                    view1File);
            }

            using (new PerformanceLogger(testOutputHelper, "Insert ENERGY_PER_BRP_GA in databricks"))
            {
                await databricksSchemaManager.InsertFromCsvFileAsync(
                    databricksSchemaManager.DeltaTableOptions.Value.ENERGY_PER_BRP_V1_VIEW_NAME,
                    EnergyPerBrpGaViewSchemaDefinition.SchemaDefinition,
                    view2File);
            }

            using (new PerformanceLogger(testOutputHelper, "Insert ENERGY_PER_ES_BRP_GA in databricks"))
            {
                await databricksSchemaManager.InsertFromCsvFileAsync(
                    databricksSchemaManager.DeltaTableOptions.Value.ENERGY_PER_ES_V1_VIEW_NAME,
                    EnergyPerEsBrpGaViewSchemaDefinition.SchemaDefinition,
                    view3File);
            }
        }
    }

    private static async Task RemoveDataForEnergySupplierInTimespan(
        MigrationsFreeDatabricksSqlStatementApiFixture fixture,
        ITestOutputHelper testOutputHelper,
        string energySupplierId,
        Instant before,
        Instant? after)
    {
        var statement = new DeleteEnergySupplierStatement(
            fixture.DatabricksSchemaManager.DeltaTableOptions.Value,
            energySupplierId,
            before,
            after);

        using (new PerformanceLogger(testOutputHelper, "Execute DeleteEnergySupplierStatement"))
        {
            await fixture.GetDatabricksExecutor().ExecuteStatementAsync(statement, Format.JsonArray).ToListAsync();
        }
    }

    private static async Task RemoveDataForCorrections(
        MigrationsFreeDatabricksSqlStatementApiFixture fixture,
        ITestOutputHelper testOutputHelper,
        IReadOnlyCollection<string> gridAreasToRemoveFrom)
    {
        foreach (var aggregationLevel in (IReadOnlyCollection<string>)[
                     DeltaTableAggregationLevel.GridArea,
                     DeltaTableAggregationLevel.BalanceResponsibleAndGridArea,
                     DeltaTableAggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea
                 ])
        {
            var statement = new DeleteCorrectionsStatement(
                fixture.DatabricksSchemaManager.DeltaTableOptions.Value,
                aggregationLevel,
                gridAreasToRemoveFrom);

            using (new PerformanceLogger(
                       testOutputHelper,
                       $"Execute DeleteCorrectionsStatement for aggregationLevel {aggregationLevel}"))
            {
                await fixture.GetDatabricksExecutor().ExecuteStatementAsync(statement, Format.JsonArray).ToListAsync();
            }
        }
    }

    private class DeleteEnergySupplierStatement(
        DeltaTableOptions deltaTableOptions,
        string energySupplierId,
        Instant before,
        Instant? after) : DatabricksStatement
    {
        private readonly DeltaTableOptions _deltaTableOptions = deltaTableOptions;
        private readonly string _energySupplierId = energySupplierId;
        private readonly Instant _before = before;
        private readonly Instant? _after = after;

        protected override string GetSqlStatement()
        {
            return $"""
                    DELETE FROM {_deltaTableOptions.WholesaleCalculationResultsSchemaName}.{_deltaTableOptions.ENERGY_PER_ES_V1_VIEW_NAME}
                    WHERE {EnergyResultColumnNames.EnergySupplierId} = '{_energySupplierId}'
                    AND {EnergyResultColumnNames.Time} <= '{_before}'
                    {(_after is not null ? $"AND {EnergyResultColumnNames.Time} > '{_after}'" : string.Empty)}
                    """;
        }
    }

    private class DeleteCorrectionsStatement(
        DeltaTableOptions deltaTableOptions,
        string aggregationLevel,
        IReadOnlyCollection<string> gridAreasToRemoveFrom) : DatabricksStatement
    {
        private readonly DeltaTableOptions _deltaTableOptions = deltaTableOptions;
        private readonly IReadOnlyCollection<string> _gridAreasToRemoveFrom = gridAreasToRemoveFrom;

        protected override string GetSqlStatement()
        {
            var tableToDeleteFrom = aggregationLevel switch
            {
                DeltaTableAggregationLevel.GridArea => _deltaTableOptions.ENERGY_V1_VIEW_NAME,
                DeltaTableAggregationLevel.BalanceResponsibleAndGridArea => _deltaTableOptions
                    .ENERGY_PER_BRP_V1_VIEW_NAME,
                DeltaTableAggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea => _deltaTableOptions
                    .ENERGY_PER_ES_V1_VIEW_NAME,
                _ => throw new InvalidOperationException(),
            };

            return $"""
                    DELETE FROM {_deltaTableOptions.SCHEMA_NAME}.{tableToDeleteFrom}
                    WHERE ({EnergyResultColumnNames.CalculationType} = '{DeltaTableCalculationType.FirstCorrectionSettlement}'
                    OR {EnergyResultColumnNames.CalculationType} = '{DeltaTableCalculationType.SecondCorrectionSettlement}'
                    OR {EnergyResultColumnNames.CalculationType} = '{DeltaTableCalculationType.ThirdCorrectionSettlement}')
                    {(_gridAreasToRemoveFrom.Any() ? $"AND {EnergyResultColumnNames.GridArea} IN ({string.Join(", ", _gridAreasToRemoveFrom.Select(ga => $"'{ga}'"))})" : string.Empty)}
                    """;
        }
    }
}
