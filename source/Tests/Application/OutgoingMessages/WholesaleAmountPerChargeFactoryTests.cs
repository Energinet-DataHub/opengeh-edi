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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.OutgoingMessages;

public class WholesaleAmountPerChargeFactoryTests
{
    [Fact]
    public void Given_DatabricksSqlRowForOneDay_When_CreateWholesaleServiceResult_Then_ReturnCorrectWholesaleServiceResult()
    {
        // Arrange
        var databricksSqlRow = new DatabricksSqlRow(
            new Dictionary<string, object?>
        {
             { WholesaleResultColumnNames.CalculationId, "e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d" },
             { WholesaleResultColumnNames.CalculationType, "WholesaleFixing" },
             { WholesaleResultColumnNames.CalculationVersion, "65" },
             { WholesaleResultColumnNames.ResultId, "17582ba4-71db-4ce5-af70-b00a4676e357" },
             { WholesaleResultColumnNames.GridAreaCode, "543" },
             { WholesaleResultColumnNames.EnergySupplierId, "7845315789" },
             { WholesaleResultColumnNames.ChargeCode, "EA001" },
             { WholesaleResultColumnNames.ChargeType, "subscription" },
             { WholesaleResultColumnNames.ChargeOwnerId, "7845315752" },
             { WholesaleResultColumnNames.Resolution, "PT1H" },
             { WholesaleResultColumnNames.QuantityUnit, "kWh" },
             { WholesaleResultColumnNames.MeteringPointType, "consumption" },
             { WholesaleResultColumnNames.SettlementMethod, "flex" },
             { WholesaleResultColumnNames.IsTax, "true" },
             { WholesaleResultColumnNames.Currency, "DKK" },
             { WholesaleResultColumnNames.Time, "2023-02-06T08:00:00.000" },
             { WholesaleResultColumnNames.Quantity, 46.572 },
             { WholesaleResultColumnNames.QuantityQualities, "[\"measured\"]" },
             { WholesaleResultColumnNames.Price,  0.756998 },
             { WholesaleResultColumnNames.Amount, 35.254911 },
        });

        var timeSeriesPoints = new List<WholesaleTimeSeriesPoint>();
        var now = Instant.FromUtc(2022, 01, 11, 23, 0, 0);
        var currentTime = now;
        var nextDay = currentTime.Plus(Duration.FromDays(1));
        while (currentTime < nextDay)
        {
            var quantity = currentTime.InUtc().Hour;
            timeSeriesPoints.Add(new WholesaleTimeSeriesPoint(
                currentTime,
                quantity,
                new List<QuantityQuality>
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Measured,
                    QuantityQuality.Calculated,
                },
                5,
                quantity * 5));
            currentTime = currentTime.Plus(Duration.FromHours(1));
        }

        // Act
        var actual = WholesaleAmountPerChargeFactory.CreatewholesaleResultForAmountPerCharge(databricksSqlRow, timeSeriesPoints);

        // Assert
        actual.Id.Should().Be("17582ba4-71db-4ce5-af70-b00a4676e357");
        actual.CalculationId.Should().Be("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
        actual.GridAreaCode.Should().Be("543");
        actual.MeteringPointType.Should().Be(MeteringPointType.Consumption);
        actual.CalculationType.Should().Be(CalculationType.WholesaleFixing);
        actual.PeriodStartUtc.Should().Be(Instant.FromUtc(2022, 01, 11, 23, 0, 0));
        actual.PeriodEndUtc.Should().Be(Instant.FromUtc(2022, 01, 12, 23, 0, 0));
        actual.Resolution.Should().Be(Resolution.Hourly);
        actual.CalculationVersion.Should().Be(65);
        actual.SettlementMethod.Should().Be(SettlementMethod.Flex);
    }
}
