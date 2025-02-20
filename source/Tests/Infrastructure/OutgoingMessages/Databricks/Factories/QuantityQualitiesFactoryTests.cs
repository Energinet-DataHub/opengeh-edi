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
using Energinet.DataHub.EDI.OutgoingMessages.Application.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Databricks.Factories;

public class QuantityQualitiesFactoryTests
{
    public static IEnumerable<object?[]> QuantityQualityWholesaleServiceMappingData()
    {
        // Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
        // Only available to Energinet employees.
        // Note that this is used for RSM-019 in the CIM format
        // QuantityQuality when price is missing is always “Missing”
        // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription” and “Fee”.
        // The following rules applies when calculation type is “Tariff”, has a price:
        /*
         * | Combination QQ from RSM-014       | Calculated quantity quality |
         * |-----------------------------------+-----------------------------|
         * | Missing + Missing                 | Missing                     |
         * | Missing + Missing                 | Missing                     |
         * | Missing + Estimated               | Incomplete                  |
         * | Missing + Measured                | Incomplete                  |
         * | Missing + Estimated + Measured    | Incomplete                  |
         * | Missing + Calculated              | Incomplete                  |
         * | Estimated + Measured              | Calculated                  |
         * | Estimated + Estimated             | Calculated                  |
         * | Estimated + Calculated            | Calculated                  |
         * | Measured + Measured               | Calculated                  |
         * | Calculated + Calculated           | Calculated                  |
         * | Measured + Estimated + Calculated | Calculated                  |
         */

        //QuantityQuality when price is missing is always “Incomplete”
        yield return new object?[]
        {
            null,
            new[]
            {
                QuantityQuality.Missing,
            },
            ChargeType.Subscription,
            CalculatedQuantityQuality.Missing,
        };

        // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription”.
        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
            },
            ChargeType.Subscription,
            CalculatedQuantityQuality.Calculated,
        };

        // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Fee”.
        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
            },
            ChargeType.Fee,
            CalculatedQuantityQuality.Calculated,
        };

        // The following test cases are defined as an input array of quantity qualities and a singular expected output.
        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Missing,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
                QuantityQuality.Estimated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Incomplete,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
                QuantityQuality.Measured,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Incomplete,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
                QuantityQuality.Estimated,
                QuantityQuality.Measured,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Incomplete,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Estimated,
                QuantityQuality.Measured,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Estimated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Measured,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Calculated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Missing,
                QuantityQuality.Calculated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Incomplete,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Estimated,
                QuantityQuality.Calculated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Estimated,
                QuantityQuality.Calculated,
                QuantityQuality.Measured,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };

        // The empty set is undefined.
        yield return new object?[]
        {
            99m,
            new List<QuantityQuality>(),
            ChargeType.Tariff,
            CalculatedQuantityQuality.NotAvailable,
        };

        // Additional test cases not based on examples from the documentation.
        yield return new object?[]
        {
            99m,
            new[]
            {
                QuantityQuality.Measured,
                QuantityQuality.Measured,
                QuantityQuality.Calculated,
            },
            ChargeType.Tariff,
            CalculatedQuantityQuality.Calculated,
        };
    }

    [Theory]
    [MemberData(nameof(QuantityQualityWholesaleServiceMappingData))]
    public void Maps_wholesale_services_quantity_quality_to_edi_quality_in_accordance_with_the_rules(
        decimal? price,
        IReadOnlyCollection<QuantityQuality> qualities,
        ChargeType? chargeType,
        CalculatedQuantityQuality expectedCalculatedQuantityQuality)
    {
        // Act
        var actual = CalculatedQuantityQualityMapper.MapForWholesaleAmountPerCharge(
            qualities,
            price != null,
            chargeType);

        // Assert
        actual.Should().Be(expectedCalculatedQuantityQuality);
    }
}
