﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.TestData;

public class WholesaleResultForAmountPerChargeInTwoGridAreasDescription
    : TestDataDescription
{
    /// <summary>
    /// Test data description for scenario using the view described by <see cref="WholesaleAmountPerChargeQuery"/>.
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
    /// All data has then been duplicated for grid area 803.
    /// </remarks>
    public WholesaleResultForAmountPerChargeInTwoGridAreasDescription()
        : base(
            "wholesale_fixing_01-02-2023_28-02-2023_ga_803_and_804_amount_per_charge_v1.csv",
            string.Empty)
    {
    }

    public override Guid CalculationId => Guid.Parse("44a9e9fd-01a9-4c37-bb09-fca2d456a414");

    public override IReadOnlyCollection<string> GridAreaCodes => new List<string>() { "803", "804" };

    public override int ExpectedCalculationResultsCount => 8;

    public int ExpectedOutgoingMessagesForSystemOperatorCount => 3;

    public int ExpectedOutgoingMessagesForGridOwnerCount => 5;

    public int ExpectedOutgoingMessagesForEnergySupplierCount => ExpectedCalculationResultsCount;

    public override Period Period => new(
        Instant.FromUtc(2023, 1, 31, 23, 0, 0),
        Instant.FromUtc(2023, 2, 28, 23, 0, 0));

    public Dictionary<string, ExampleWholesaleResultMessageForActor> ExampleWholesaleResultMessageDataForEnergySupplier
        => new Dictionary<string, ExampleWholesaleResultMessageForActor>()
        {
            {
                "803", new(
                    GridArea: "803",
                    Currency.DanishCrowns,
                    ActorNumber.Create("5790001662233"),
                    MeteringPointType.Consumption,
                    SettlementMethod.Flex,
                    Resolution.Daily,
                    65,
                    Points: TimeSeriesPointsFactory
                        .CreatePointsForPeriod(Period, Resolution.Daily, 0.348m, 2, 0.697M, CalculatedQuantityQuality.Calculated),
                    ChargeCode: "Sub-804",
                    ChargeType: ChargeType.Subscription,
                    MeasurementUnit: MeasurementUnit.Pieces)
            },
            {
                "804", new(
                    GridArea: "803",
                    Currency.DanishCrowns,
                    ActorNumber.Create("5790001662233"),
                    MeteringPointType.Consumption,
                    SettlementMethod.Flex,
                    Resolution.Daily,
                    65,
                    Points: TimeSeriesPointsFactory
                        .CreatePointsForPeriod(Period, Resolution.Daily, 0.348m, 2, 0.697M, CalculatedQuantityQuality.Calculated),
                    ChargeCode: "Sub-804",
                    ChargeType: ChargeType.Subscription,
                    MeasurementUnit: MeasurementUnit.Pieces)
            },
        };

    public Dictionary<string, ExampleWholesaleResultMessageForActor> ExampleWholesaleResultMessageDataForSystemOperator
        => new Dictionary<string, ExampleWholesaleResultMessageForActor>()
        {
            {
                "803", new(
                    GridArea: "803",
                    Currency.DanishCrowns,
                    ActorNumber.Create("5790001662233"),
                    MeteringPointType.Consumption,
                    SettlementMethod.Flex,
                    Resolution.Daily,
                    65,
                    Points: new List<WholesaleServicesPoint>()
                    {
                        new(1, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(2, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(3, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(4, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(5, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(6, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(7, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(8, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(9, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(10, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(11, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(12, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(13, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(14, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(15, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(16, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(17, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(18, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(19, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(21, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(22, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(23, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(24, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(25, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(26, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(27, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(28, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(29, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                    },
                    ChargeCode: "41000",
                    ChargeType: ChargeType.Tariff,
                    MeasurementUnit: MeasurementUnit.KilowattHour)
            },
            {
                "804", new(
                    GridArea: "804",
                    Currency.DanishCrowns,
                    ActorNumber.Create("5790001662233"),
                    MeteringPointType.Consumption,
                    SettlementMethod.Flex,
                    Resolution.Daily,
                    65,
                    Points: new List<WholesaleServicesPoint>()
                    {
                        new(1, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(2, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(3, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(4, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(5, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(6, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(7, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(8, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(9, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(10, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(11, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(12, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(13, 1117.728m, 1.757m, 1963.846m, CalculatedQuantityQuality.Calculated),
                        new(14, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(15, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(16, 0.000m, 1.757m, 0.000m, CalculatedQuantityQuality.Missing),
                        new(17, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(18, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(19, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(21, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(22, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(23, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(24, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(25, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(26, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(27, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(28, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                        new(29, 1002.720m, 1.757m, 1761.777m, CalculatedQuantityQuality.Calculated),
                    },
                    ChargeCode: "41000",
                    ChargeType: ChargeType.Tariff,
                    MeasurementUnit: MeasurementUnit.KilowattHour)
            },
        };

    public ImmutableDictionary<string, ActorNumber> GridAreaOwners =>
        GridAreaCodes.ToImmutableDictionary(
            gridAreaCode => gridAreaCode,
            _ => ActorNumber.Create("8500000000502"));
}
