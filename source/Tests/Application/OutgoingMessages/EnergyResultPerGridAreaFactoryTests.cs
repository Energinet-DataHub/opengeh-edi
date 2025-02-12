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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.OutgoingMessages;

public class EnergyResultPerGridAreaFactoryTests
{
    [Fact]
    public void Given_DatabricksSqlRowForOneDay_When_CreateEnergyResult_Then_ReturnCorrectEnergyResultPerGridArea()
    {
        // Arrange
        var databricksSqlRow = new DatabricksSqlRow(
            new Dictionary<string, object?>
        {
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationId, "e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationType, "balance_fixing" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationVersion, "63" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.ResultId, "17582ba4-71db-4ce5-af70-b00a4676e357" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.GridAreaCode, "543" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.MeteringPointType, "consumption" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.SettlementMethod, null },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.Resolution, "PT1H" },
            { EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.QuantityUnit, "kWh" },
        });

        var timeSeriesPoints = new List<EnergyTimeSeriesPoint>()
        {
            new(
                Instant.FromUtc(2022, 01, 11, 23, 0, 0),
                281,
                new List<QuantityQuality>
                {
                    QuantityQuality.Missing,
                }),
        };

        for (int i = 0; i < 23; i++)
        {
            timeSeriesPoints.Add(new EnergyTimeSeriesPoint(
                Instant.FromUtc(2022, 01, 12, i, 0, 0),
                282 + i,
                new List<QuantityQuality>
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Measured,
                    QuantityQuality.Calculated,
                }));
        }

        // Act
        var actual = CreateEnergyResultPerGridArea(databricksSqlRow, timeSeriesPoints);

        // Assert
        actual.Id.Should().Be("17582ba4-71db-4ce5-af70-b00a4676e357");
        actual.CalculationId.Should().Be("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
        actual.GridAreaCode.Should().Be("543");
        actual.MeteringPointType.Should().Be(MeteringPointType.Consumption);
        actual.CalculationType.Should().Be(EDI.OutgoingMessages.Interfaces.Models.CalculationResults.CalculationType.BalanceFixing);
        actual.PeriodStartUtc.Should().Be(Instant.FromUtc(2022, 01, 11, 23, 0, 0));
        actual.PeriodEndUtc.Should().Be(Instant.FromUtc(2022, 01, 12, 23, 0, 0));
        actual.Resolution.Should().Be(Resolution.Hourly);
        actual.CalculationVersion.Should().Be(63);
        actual.SettlementMethod.Should().BeNull();
        actual.MeasureUnitType.Should().Be(MeasurementUnit.KilowattHour);
    }

    private static EnergyResultPerGridArea CreateEnergyResultPerGridArea(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        var resolution = ResolutionMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.Resolution));

        var period = PeriodFactory.GetPeriod(timeSeriesPoints, resolution);

        return new EnergyResultPerGridArea(
            databricksSqlRow.ToGuid(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.ResultId),
            databricksSqlRow.ToGuid(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationId),
            databricksSqlRow.ToNonEmptyString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.GridAreaCode),
            MeteringPointTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.MeteringPointType)),
            timeSeriesPoints,
            CalculationTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationType)),
            period.Start,
            period.End,
            resolution,
            databricksSqlRow.ToLong(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.CalculationVersion),
            SettlementMethodMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.SettlementMethod)),
            MeasurementUnitMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries.EnergyResultColumnNames.QuantityUnit)));
    }
}
