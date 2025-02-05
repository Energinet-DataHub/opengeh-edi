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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.Edi.Responses;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;

public class WholesaleResultForAmountPerChargeDescription
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
    /// </remarks>
    public WholesaleResultForAmountPerChargeDescription()
        : base(
            "wholesale_fixing_01-02-2023_28-02-2023_ga_804_amount_per_charge_v1.csv",
            //Charge type on row 1457 contains an invalid value (="invalid") (row on a result set)
            "wholesale_fixing_01-02-2023_28-02-2023_ga_804_amount_per_charge_v1_with_invalid_row.csv")
    {
    }

    public override Guid CalculationId => Guid.Parse("44a9e9fd-01a9-4c37-bb09-fca2d456a414");

    public override string GridAreaCode => "804";

    public override int ExpectedCalculationResultsCount => 8;

    public int ExpectedOutgoingMessagesForSystemOperatorCount => 3;

    public int ExpectedOutgoingMessagesForGridOwnerCount => 5;

    public int ExpectedOutgoingMessagesForEnergySupplierCount => ExpectedCalculationResultsCount;

    public override Period Period => new(
        Instant.FromUtc(2023, 1, 31, 23, 0, 0),
        Instant.FromUtc(2023, 2, 28, 23, 0, 0));

    public ExampleWholesaleResultMessageForActor ExampleWholesaleResultMessageData => new(
        GridArea: GridAreaCode,
        Currency.DanishCrowns,
        ActorNumber.Create("5790001662233"),
        MeteringPointType.Consumption,
        SettlementMethod.Flex,
        Resolution.Daily,
        65,
        Points: TimeSeriesPointsFactory
            .CreatePointsForPeriod(Period, Resolution.Daily, 0.348m, 2, 0.697M, QuantityQuality.Calculated),
        ChargeCode: "Sub-804",
        ChargeType: ChargeType.Subscription,
        MeasurementUnit: MeasurementUnit.Pieces);

    public ExampleWholesaleResultMessageForActor ExampleWholesaleResultMessageDataForSystemOperator => new(
        GridArea: GridAreaCode,
        Currency.DanishCrowns,
        ActorNumber.Create("5790001662233"),
        MeteringPointType.Consumption,
        SettlementMethod.Flex,
        Resolution.Daily,
        65,
        Points: new List<WholesaleServicesRequestSeries.Types.Point>()
        {
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1117.728m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1963.846m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(0.000m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(0.000m), QuantityQualities = { QuantityQuality.Missing } },
            new() { Quantity = DecimalValue.FromDecimal(0.000m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(0.000m), QuantityQualities = { QuantityQuality.Missing } },
            new() { Quantity = DecimalValue.FromDecimal(0.000m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(0.000m), QuantityQualities = { QuantityQuality.Missing } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
            new() { Quantity = DecimalValue.FromDecimal(1002.720m), Price = DecimalValue.FromDecimal(1.757m), Amount = DecimalValue.FromDecimal(1761.777m), QuantityQualities = { QuantityQuality.Calculated } },
        },
        ChargeCode: "41000",
        ChargeType: ChargeType.Tariff,
        MeasurementUnit: MeasurementUnit.Kwh);

    public ImmutableDictionary<string, ActorNumber> GridAreaOwners =>
        ImmutableDictionary<string, ActorNumber>.Empty
            .Add(GridAreaCode, ActorNumber.Create("8500000000502"));
}
