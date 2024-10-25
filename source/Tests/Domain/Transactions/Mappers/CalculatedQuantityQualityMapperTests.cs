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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument.

namespace Energinet.DataHub.EDI.Tests.Domain.Transactions.Mappers;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test class")]
public sealed class CalculatedQuantityQualityMapperTests
{
    private static readonly QuantityQuality[][] _quantityQualityPowerSet = FastPowerSet(
        Enum.GetValues(typeof(QuantityQuality))
            .Cast<QuantityQuality>()
            .ToArray());

    // Taken from https://stackoverflow.com/questions/19890781/creating-a-power-set-of-a-sequence
    // by user https://stackoverflow.com/users/1740808/sergeys
    private static T[][] FastPowerSet<T>(IReadOnlyList<T> seq)
    {
        var powerSet = new T[1 << seq.Count][];
        powerSet[0] = Array.Empty<T>(); // starting only with empty set

        for (var i = 0; i < seq.Count; i++)
        {
            var cur = seq[i];
            var count = 1 << i; // doubling list each time
            for (var j = 0; j < count; j++)
            {
                var source = powerSet[j];
                var destination = powerSet[count + j] = new T[source.Length + 1];
                for (var q = 0; q < source.Length; q++)
                    destination[q] = source[q];
                destination[source.Length] = cur;
            }
        }

        return powerSet;
    }

    public sealed class EnergyTests
    {
        [Fact]
        public void Given_EnergyQuantityQualityTestCases_When_Unpacked_Then_ContainsAllPossibleCases()
        {
            new EnergyQuantityQualityTestCases()
                .Select(testCase => testCase[0])
                .Should()
                .BeEquivalentTo(_quantityQualityPowerSet);
        }

        [Fact]
        public void Given_EnergyQuantityQualityTestCases_When_Unpacked_Then_ContainsExamplesFromBusinessDocumentation()
        {
            /*
             * Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
             * Only available to Energinet employees.
             * Note that this is used for RSM-014 in the CIM format
             *
             * | Combination QQ from RSM-014       | Calculated quantity quality |
             * |-----------------------------------+-----------------------------|
             * | Missing + Missing                 | Missing                     |
             * | Missing + Estimated               | Incomplete                  |
             * | Missing + Measured                | Incomplete                  |
             * | Missing + Estimated + Measured    | Incomplete                  |
             * | Estimated + Measured              | Estimated                   |
             * | Estimated + Estimated             | Estimated                   |
             * | Measured + Measured               | Measured                    |
             * | Calculated + Calculated           | Calculated                  |
             * | Missing + Calculated              | Incomplete                  |
             * | Estimated + Calculated            | Estimated                   |
             * | Measured + Estimated + Calculated | Estimated                   |
             */

            var energyQuantityQualityTestCases = new EnergyQuantityQualityTestCases();

            energyQuantityQualityTestCases
                .Should()
                .ContainEquivalentOf(
                    new object[] { new[] { QuantityQuality.Missing }, CalculatedQuantityQuality.Missing, })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Missing, QuantityQuality.Estimated },
                        CalculatedQuantityQuality.Incomplete,
                    })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Missing, QuantityQuality.Measured },
                        CalculatedQuantityQuality.Incomplete,
                    })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Missing, QuantityQuality.Estimated, QuantityQuality.Measured },
                        CalculatedQuantityQuality.Incomplete,
                    })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Estimated, QuantityQuality.Measured },
                        CalculatedQuantityQuality.Estimated,
                    })
                .And.ContainEquivalentOf(
                    new object[] { new[] { QuantityQuality.Estimated }, CalculatedQuantityQuality.Estimated, })
                .And.ContainEquivalentOf(
                    new object[] { new[] { QuantityQuality.Measured }, CalculatedQuantityQuality.Measured, })
                .And.ContainEquivalentOf(
                    new object[] { new[] { QuantityQuality.Calculated }, CalculatedQuantityQuality.Calculated, })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Missing, QuantityQuality.Calculated },
                        CalculatedQuantityQuality.Incomplete,
                    })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Estimated, QuantityQuality.Calculated },
                        CalculatedQuantityQuality.Estimated,
                    })
                .And.ContainEquivalentOf(
                    new object[]
                    {
                        new[] { QuantityQuality.Measured, QuantityQuality.Estimated, QuantityQuality.Calculated },
                        CalculatedQuantityQuality.Estimated,
                    });
        }

        [Theory]
        [ClassData(typeof(EnergyQuantityQualityTestCases))]
        public void
            Given_QuantityQualitySetForEnergyFromWholesale_When_MappedToEdiQuantityQuality_Then_MappingIsCorrect(
                ICollection<QuantityQuality> quantityQualitiesFromWholesale,
                CalculatedQuantityQuality expectedCalculatedQuantityQuality)
        {
            // Act
            var actual = CalculatedQuantityQualityMapper.MapForEnergyResults(quantityQualitiesFromWholesale);

            // Assert
            actual.Should().Be(expectedCalculatedQuantityQuality);
        }

        private sealed class EnergyQuantityQualityTestCases
            : TheoryData<IReadOnlyCollection<QuantityQuality>, CalculatedQuantityQuality>
        {
            public EnergyQuantityQualityTestCases()
            {
                // Missing cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => qqs.Contains(QuantityQuality.Missing))
                             .Where(
                                 qqs => qqs.Length == 1
                                        || (qqs.Length == 2 && qqs.Contains(QuantityQuality.Unspecified))))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.Missing);
                }

                // Incomplete cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => qqs.Contains(QuantityQuality.Missing))
                             .Where(
                                 qqs => qqs.Length > 2
                                        || (qqs.Length > 1 && !qqs.Contains(QuantityQuality.Unspecified))))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.Incomplete);
                }

                // Estimated cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => qqs.Contains(QuantityQuality.Estimated))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Missing)))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.Estimated);
                }

                // Measured cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => qqs.Contains(QuantityQuality.Measured))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Estimated)))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.Measured);
                }

                // Calculated cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => qqs.Contains(QuantityQuality.Calculated))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Estimated))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Measured)))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.Calculated);
                }

                // Not available cases
                foreach (var quantityQualities in _quantityQualityPowerSet
                             .Where(qqs => !qqs.Contains(QuantityQuality.Missing))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Estimated))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Measured))
                             .Where(qqs => !qqs.Contains(QuantityQuality.Calculated)))
                {
                    Add(quantityQualities, CalculatedQuantityQuality.NotAvailable);
                }
            }
        }
    }

    public sealed class WholesaleTests
    {
        [Fact]
        public void Given_WholesaleQuantityQualityTestCases_When_Unpacked_Then_ContainAllValuesAtLeastOnce()
        {
            var wholesaleQuantityQualityTestCases = new WholesaleQuantityQualityTestCases();

            using var assertionScope = new AssertionScope();

            // Quantity qualities
            foreach (var quantityQuality in Enum.GetValues<QuantityQuality>())
            {
                wholesaleQuantityQualityTestCases
                    .Select(testCase => testCase[0])
                    .Should()
                    .Contain(qqs => ((ICollection<QuantityQuality>)qqs).Contains(quantityQuality));
            }

            // Resolutions
            foreach (var resolution in Enum.GetValues<WholesaleServicesRequestSeries.Types.Resolution>())
            {
                wholesaleQuantityQualityTestCases
                    .Select(testCase => testCase[1])
                    .Distinct()
                    .Should()
                    .Contain(resolution);
            }

            // Has price
            foreach (var hasPrice in new[] { true, false })
            {
                wholesaleQuantityQualityTestCases
                    .Select(testCase => testCase[2])
                    .Distinct()
                    .Should()
                    .Contain(hasPrice);
            }

            // Charge types
            foreach (var chargeType in Enum.GetValues<WholesaleServicesRequestSeries.Types.ChargeType>())
            {
                wholesaleQuantityQualityTestCases
                    .Select(testCase => testCase[3])
                    .Distinct()
                    .Should()
                    .Contain(chargeType);
            }
        }

        [Theory]
        [ClassData(typeof(WholesaleQuantityQualityResolutionHasPriceChargeTypeInputTestCases))]
        public void
            Given_WholesaleQuantityQualityResolutionHasPriceChargeTypeInput_When_MappedToEdiQuantityQuality_Then_CanMapWithoutError(
                ICollection<QuantityQuality> qualitySetFromWholesale,
                WholesaleServicesRequestSeries.Types.Resolution resolution,
                bool hasPrice,
                WholesaleServicesRequestSeries.Types.ChargeType chargeType)
        {
            var act = () => CalculatedQuantityQualityMapper.MapForWholesaleServices(
                qualitySetFromWholesale,
                resolution,
                hasPrice,
                chargeType);

            act.Should().NotThrow();
        }

        [Theory]
        [ClassData(typeof(WholesaleQuantityQualityTestCases))]
        public void
            Given_QuantityQualitySetForWholesaleFromWholesale_When_MappedToEdiQuantityQuality_Then_MappingIsCorrect(
                ICollection<QuantityQuality> qualitySetFromWholesale,
                WholesaleServicesRequestSeries.Types.Resolution resolution,
                bool hasPrice,
                WholesaleServicesRequestSeries.Types.ChargeType chargeType,
                CalculatedQuantityQuality? expectedCalculatedQuantityQuality)
        {
            // Act
            var actual = CalculatedQuantityQualityMapper.MapForWholesaleServices(
                qualitySetFromWholesale,
                resolution,
                hasPrice,
                chargeType);

            // Assert
            actual.Should().Be(expectedCalculatedQuantityQuality);
        }

        private sealed class WholesaleQuantityQualityTestCases
            : TheoryData<
                ICollection<QuantityQuality>,
                WholesaleServicesRequestSeries.Types.Resolution,
                bool,
                WholesaleServicesRequestSeries.Types.ChargeType,
                CalculatedQuantityQuality?>
        {
            public WholesaleQuantityQualityTestCases()
            {
                /* Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
                 * Only available to Energinet employees.
                 * Note that this is used for RSM-019 in the CIM format
                 * QuantityQuality for monthly amount is always “null”
                 * QuantityQuality when price is missing is always “Missing”
                 * If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription” and “Fee”.
                 * The following rules applies when calculation type is “Tariff”, has a price and is not monthly:
                 *
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

                // QuantityQuality for monthly amount is always “null”
                Add(
                    [QuantityQuality.Missing],
                    WholesaleServicesRequestSeries.Types.Resolution.Monthly,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                    null);

                // QuantityQuality, when price is missing is always “Missing”
                Add(
                    [QuantityQuality.Missing],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    false,
                    WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                    CalculatedQuantityQuality.Missing);

                // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription”.
                Add(
                    [QuantityQuality.Missing],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                    CalculatedQuantityQuality.Calculated);

                // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Fee”.
                Add(
                    [QuantityQuality.Missing],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Fee,
                    CalculatedQuantityQuality.Calculated);

                // Test cases from the documentation above (with some additional cases)
                Add(
                    [QuantityQuality.Missing],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Missing);

                Add(
                    [QuantityQuality.Estimated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Measured],
                    WholesaleServicesRequestSeries.Types.Resolution.Hour,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Estimated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Measured],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Hour,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Estimated, QuantityQuality.Measured],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Estimated, QuantityQuality.Measured],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Estimated, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Hour,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Measured, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                Add(
                    [QuantityQuality.Estimated, QuantityQuality.Calculated, QuantityQuality.Measured],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                // Duplicated quantity qualities does not alter the result.
                Add(
                    [QuantityQuality.Measured, QuantityQuality.Measured, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);

                // The empty set is undefined.
                Add(
                    [],
                    WholesaleServicesRequestSeries.Types.Resolution.Hour,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.NotAvailable);

                // Unspecified cases for resolution, charge type, and quantity quality.
                // These are considered not-monthly, not-subscription and not-fee, and ignored respectively.
                Add(
                    [QuantityQuality.Unspecified],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.NotAvailable);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Unspecified,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Unspecified,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Missing, QuantityQuality.Calculated],
                    WholesaleServicesRequestSeries.Types.Resolution.Unspecified,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Unspecified,
                    CalculatedQuantityQuality.Incomplete);

                Add(
                    [QuantityQuality.Unspecified],
                    WholesaleServicesRequestSeries.Types.Resolution.Unspecified,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Unspecified,
                    CalculatedQuantityQuality.NotAvailable);

                // Unspecified quantity quality is ignored
                Add(
                    [QuantityQuality.Missing, QuantityQuality.Unspecified],
                    WholesaleServicesRequestSeries.Types.Resolution.Hour,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Missing);

                Add(
                    [QuantityQuality.Estimated, QuantityQuality.Unspecified],
                    WholesaleServicesRequestSeries.Types.Resolution.Day,
                    true,
                    WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                    CalculatedQuantityQuality.Calculated);
            }
        }

        private sealed class WholesaleQuantityQualityResolutionHasPriceChargeTypeInputTestCases
            : TheoryData<
                ICollection<QuantityQuality>,
                WholesaleServicesRequestSeries.Types.Resolution,
                bool,
                WholesaleServicesRequestSeries.Types.ChargeType>
        {
            public WholesaleQuantityQualityResolutionHasPriceChargeTypeInputTestCases()
            {
                var resolutions = Enum.GetValues<WholesaleServicesRequestSeries.Types.Resolution>();
                var chargeTypes = Enum.GetValues<WholesaleServicesRequestSeries.Types.ChargeType>();
                var hasPrice = new[] { true, false };

                foreach (var resolution in resolutions)
                {
                    foreach (var chargeType in chargeTypes)
                    {
                        foreach (var price in hasPrice)
                        {
                            foreach (var qualitySet in _quantityQualityPowerSet)
                            {
                                Add(
                                    qualitySet,
                                    resolution,
                                    price,
                                    chargeType);
                            }
                        }
                    }
                }
            }
        }
    }
}
