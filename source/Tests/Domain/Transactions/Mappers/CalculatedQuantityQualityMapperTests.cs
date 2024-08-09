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
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument.

namespace Energinet.DataHub.EDI.Tests.Domain.Transactions.Mappers;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test class")]
public sealed class CalculatedQuantityQualityMapperTests
{
    public sealed class EdiResponseTests
    {
        public static IEnumerable<object[]> QuantityQualityEnergyResultMappingData()
        {
            // Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
            // Only available to Energinet employees.
            // Note that this is used for RSM-014
            /*
             * | Combination QQ from RSM-012       | Calculated quantity quality |
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

            // The following test cases are defined as an input array of quantity qualities and a singular expected output.
            yield return new object[]
            {
                new[] { QuantityQuality.Missing }, CalculatedQuantityQuality.Missing,
            };

            yield return new object[]
            {
                new[]
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Estimated,
                },
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object[]
            {
                new[]
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Measured,
                },
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object[]
            {
                new[]
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Estimated,
                    QuantityQuality.Measured,
                },
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object[]
            {
                new[]
                {
                    QuantityQuality.Estimated,
                    QuantityQuality.Measured,
                },
                CalculatedQuantityQuality.Estimated,
            };

            yield return new object[]
            {
                new[] { QuantityQuality.Estimated }, CalculatedQuantityQuality.Estimated,
            };

            yield return new object[] { new[] { QuantityQuality.Measured }, CalculatedQuantityQuality.Measured };

            yield return new object[] { new[] { QuantityQuality.Calculated }, CalculatedQuantityQuality.Calculated };

            yield return new object[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Calculated },
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object[]
            {
                new[] { QuantityQuality.Estimated, QuantityQuality.Calculated },
                CalculatedQuantityQuality.Estimated,
            };

            yield return new object[]
            {
                new[] { QuantityQuality.Estimated, QuantityQuality.Calculated, QuantityQuality.Measured },
                CalculatedQuantityQuality.Estimated,
            };

            // The empty set is undefined.
            yield return new object[] { new List<QuantityQuality>(), CalculatedQuantityQuality.NotAvailable };

            // Additional test cases not based on examples from the documentation.
            yield return new object[]
            {
                new[] { QuantityQuality.Measured, QuantityQuality.Measured, QuantityQuality.Calculated },
                CalculatedQuantityQuality.Measured,
            };
        }

        public static IEnumerable<object?[]> QuantityQualityWholesaleServiceMappingData()
        {
            // Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
            // Only available to Energinet employees.
            // Note that this is used for RSM-019
            // QuantityQuality for monthly amount is always “null”
            // If QuantityQuality is not monthly amount, the following rules apply:
            /*
             * | Combination QQ from RSM-012       | Calculated quantity quality |
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

            //QuantityQuality for monthly amount is always “Calculated”
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Monthly,
                null,
            };

            // The following test cases are defined as an input array of quantity qualities and a singular expected output.
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Missing,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Estimated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Estimated, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Estimated, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Estimated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Calculated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[]
                {
                    QuantityQuality.Missing,
                    QuantityQuality.Calculated,
                },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[]
                {
                    QuantityQuality.Estimated,
                    QuantityQuality.Calculated,
                },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[]
                {
                    QuantityQuality.Estimated,
                    QuantityQuality.Calculated,
                    QuantityQuality.Measured,
                },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };

            // The empty set is undefined.
            yield return new object?[]
            {
                new List<QuantityQuality>(),
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.NotAvailable,
            };

            // Additional test cases not based on examples from the documentation.
            yield return new object?[]
            {
                new[]
                {
                    QuantityQuality.Measured,
                    QuantityQuality.Measured,
                    QuantityQuality.Calculated,
                },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                CalculatedQuantityQuality.Calculated,
            };
        }

        public static IEnumerable<object[]> QuantityQualityValues()
        {
            return Enum.GetValues<QuantityQuality>().Select(qq => new[] { (object)qq });
        }

        [Fact]
        public void Given_nullCollection_When_MapForEnergyResults_Then_ThrowsArgumentNullException()
        {
            // Arrange
            ICollection<QuantityQuality>? quality = null;

            // Act & Assert
            var act = () => CalculatedQuantityQualityMapper.MapForEnergyResults(quality);
            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Given_NullCollection_When_MapForWholesaleServices_Then_ThrowsArgumentNullException()
        {
            // Arrange
            ICollection<QuantityQuality>? quality = null;

            // Act & Assert
            var act = () => CalculatedQuantityQualityMapper.MapForWholesaleServices(quality, WholesaleServicesRequestSeries.Types.Resolution.Day);
            act.Should().ThrowExactly<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(QuantityQualityEnergyResultMappingData))]
        public void Given_EnergyResultQuantityQuality_When_MapForEnergyResults_Then_MapsToCorrectEdiQuality(
            ICollection<QuantityQuality> qualitySetFromWholesale,
            CalculatedQuantityQuality expectedCalculatedQuantityQuality)
        {
            // Act
            var actual = CalculatedQuantityQualityMapper.MapForEnergyResults(qualitySetFromWholesale);

            // Assert
            actual.Should().Be(expectedCalculatedQuantityQuality);
        }

        [Theory]
        [MemberData(nameof(QuantityQualityWholesaleServiceMappingData))]
        public void Maps_wholesale_services_quantity_quality_to_edi_quality_in_accordance_with_the_rules(
            ICollection<QuantityQuality> qualitySetFromWholesale,
            WholesaleServicesRequestSeries.Types.Resolution resolution,
            CalculatedQuantityQuality? expectedCalculatedQuantityQuality)
        {
            // Act
            var actual = CalculatedQuantityQualityMapper.MapForWholesaleServices(
                qualitySetFromWholesale,
                resolution);

            // Assert
            actual.Should().Be(expectedCalculatedQuantityQuality);
        }

        [Theory]
        [MemberData(nameof(QuantityQualityValues))]
        public void Can_handle_all_quantity_qualities(QuantityQuality quantityQuality)
        {
            // Act
            CalculatedQuantityQualityMapper.MapForEnergyResults(new[] { quantityQuality });
            CalculatedQuantityQualityMapper.MapForWholesaleServices(
                new[] { quantityQuality },
                WholesaleServicesRequestSeries.Types.Resolution.Day);
        }
    }
}
