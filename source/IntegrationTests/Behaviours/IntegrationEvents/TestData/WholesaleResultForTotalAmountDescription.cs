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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;

public class WholesaleResultForTotalAmountDescription
    : TestDataDescription
{
    /// <summary>
    /// Test data description for scenario using the view described by <see cref="WholesaleTotalAmountQuery"/>.
    /// </summary>
    /// <remarks>
    /// Test data is exported from Databricks using the view 'wholesale_results_amount_per_charge'
    ///    select * from view
    ///    where grid_area_code = 804
    ///    and time greaterThanOrEqual '2023-01-31T23:00:00.000'
    ///    and time smallerThan '2023-02-28T23:00:00.000'
    ///    and energy_supplier_id = '5790001662233'
    ///    and calculation_version  = 65
    /// Environment: Dev002.
    /// </remarks>
    public WholesaleResultForTotalAmountDescription()
        : base("wholesale_fixing_01-02-2023_28-02-2023_ga_804_total_amount_v1.csv")
    {
    }

    public override Guid CalculationId => Guid.Parse("44a9e9fd-01a9-4c37-bb09-fca2d456a414");

    public override string GridAreaCode => "804";

    public override int ExpectedCalculationResultsCount => 3;

    public int ExpectedOutgoingMessagesForSystemOperatorCount => 1;

    public int ExpectedOutgoingMessagesForGridOwnerCount => 1;

    public int ExpectedOutgoingMessagesForEnergySupplierCount => 1;

    public override Period Period => new(
        Instant.FromUtc(2023, 1, 31, 23, 0, 0),
        Instant.FromUtc(2023, 2, 28, 23, 0, 0));

    public ExampleWholesaleResultMessageForActor ExampleWholesaleResultMessageDataForSystemOperator => new(
        GridArea: GridAreaCode,
        Currency.DanishCrowns,
        ActorNumber.Create("5790001662233"),
        null,
        null,
        Resolution.Monthly,
        65,
        Points: TimeSeriesPointsFactory
            .CreatePointsForPeriod(Period, Resolution.Monthly, null, null, 128533.784M, null));

    public ExampleWholesaleResultMessageForActor ExampleWholesaleResultMessageDataForChargeOwner => new(
        GridArea: GridAreaCode,
        Currency.DanishCrowns,
        ActorNumber.Create("5790001662233"),
        null,
        null,
        Resolution.Monthly,
        65,
        Points: TimeSeriesPointsFactory
            .CreatePointsForPeriod(Period, Resolution.Monthly, null, null, 220886.159m, null));

    public ExampleWholesaleResultMessageForActor ExampleWholesaleResultMessageDataForEnergySupplier => new(
        GridArea: GridAreaCode,
        Currency.DanishCrowns,
        ActorNumber.Create("5790001662233"),
        null,
        null,
        Resolution.Monthly,
        65,
        Points: TimeSeriesPointsFactory
            .CreatePointsForPeriod(Period, Resolution.Monthly, null, null, 349419.943m, null));
}
