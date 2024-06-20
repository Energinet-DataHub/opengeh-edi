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
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;

/// <summary>
/// Test data description for scenario using the view described by <see cref="EnergyResultPerGridAreaQuery"/>.
/// </summary>
public class EnergyResultPerGridAreaDescription
    : TestDataDescription
{
    public EnergyResultPerGridAreaDescription()
        : base("balance_fixing_01-11-2022_01-12-2022_ga_543_per_ga_v1.csv")
    {
    }

    public override Guid CalculationId => Guid.Parse("a8cfe7c7-f197-405c-b922-52153fa0332d");

    public override string GridAreaCode => "543";

    public override int ExpectedCalculationResultsCount => 5;

    public override int ExpectedOutgoingMessagesCount => 5;

    public override Period Period => new(
        Instant.FromUtc(2022, 1, 11, 23, 0, 0),
        Instant.FromUtc(2022, 1, 12, 23, 0, 0));

    public ExampleEnergyResultMessageForActor ExampleEnergyResultMessageData => new(
        GridArea: GridAreaCode,
        MeteringPointType.Consumption,
        SettlementMethod.Flex,
        Resolution.Hourly,
        null,
        111,
        TimeSeriesPointsFactory.CreatePointsForDay(
            Period.Start,
            1928898.153m,
            CalculatedQuantityQuality.Incomplete));
}
