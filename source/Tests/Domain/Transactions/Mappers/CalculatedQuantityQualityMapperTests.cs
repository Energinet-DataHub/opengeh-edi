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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;
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
            // Note that this is used for RSM-014 in the CIM format
            /*
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

        public static IEnumerable<object?[]> QuantityQualityWholesaleServiceRequestMappingData()
        {
            // Example mappings from the documentation at https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality.
            // Only available to Energinet employees.
            // Note that this is used for RSM-019 in the CIM format
            // QuantityQuality for monthly amount is always “null”
            // QuantityQuality when price is missing is always “Missing”
            // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription” and “Fee”.
            // The following rules applies when calculation type is “Tariff”, has a price and is not monthly:
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

            //QuantityQuality for monthly amount is always “Calculated”
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Monthly,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                null,
            };

            //QuantityQuality when price is missing is always “Incomplete”
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                false,
                WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                CalculatedQuantityQuality.Missing,
            };

            // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription”.
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Subscription,
                CalculatedQuantityQuality.Calculated,
            };

            // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Fee”.
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Fee,
                CalculatedQuantityQuality.Calculated,
            };

            // The following test cases are defined as an input array of quantity qualities and a singular expected output.
            yield return new object?[]
            {
                new[] { QuantityQuality.Missing },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Missing,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Estimated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Missing, QuantityQuality.Estimated, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Estimated, QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Estimated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Measured },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                new[] { QuantityQuality.Calculated },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
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
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
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
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
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
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            // The empty set is undefined.
            yield return new object?[]
            {
                new List<QuantityQuality>(),
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
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
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };
        }

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
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing },
                ChargeType.Subscription,
                CalculatedQuantityQuality.Missing,
            };

            // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Subscription”.
            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing },
                ChargeType.Subscription,
                CalculatedQuantityQuality.Calculated,
            };

            // If Price is present, the QuantityQuality is “Calculated” if the CalculationType is “Fee”.
            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing },
                ChargeType.Fee,
                CalculatedQuantityQuality.Calculated,
            };

            // The following test cases are defined as an input array of quantity qualities and a singular expected output.
            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Missing,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                99m,
                new[] { Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Calculated },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Missing,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Calculated,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Incomplete,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Calculated,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Estimated,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Calculated,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                },
                ChargeType.Tariff,
                CalculatedQuantityQuality.Calculated,
            };

            // The empty set is undefined.
            yield return new object?[]
            {
                99m,
                new List<Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality>(),
                ChargeType.Tariff,
                CalculatedQuantityQuality.NotAvailable,
            };

            // Additional test cases not based on examples from the documentation.
            yield return new object?[]
            {
                99m,
                new[]
                {
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Measured,
                    Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality.Calculated,
                },
                ChargeType.Tariff,
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
            var act = () => CalculatedQuantityQualityMapper.MapForWholesaleServices(quality, WholesaleServicesRequestSeries.Types.Resolution.Day, true, WholesaleServicesRequestSeries.Types.ChargeType.Tariff);
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
        [MemberData(nameof(QuantityQualityWholesaleServiceRequestMappingData))]
        public void Maps_wholesale_services_request_quantity_quality_to_edi_quality_in_accordance_with_the_rules(
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

        [Theory]
        [MemberData(nameof(QuantityQualityValues))]
        public void Can_handle_all_quantity_qualities(QuantityQuality quantityQuality)
        {
            // Act
            CalculatedQuantityQualityMapper.MapForEnergyResults(new[] { quantityQuality });
            CalculatedQuantityQualityMapper.MapForWholesaleServices(
                new[] { quantityQuality },
                WholesaleServicesRequestSeries.Types.Resolution.Day,
                true,
                WholesaleServicesRequestSeries.Types.ChargeType.Tariff);
        }

        [Theory]
        [MemberData(nameof(QuantityQualityWholesaleServiceMappingData))]
        public void Maps_wholesale_services_quantity_quality_to_edi_quality_in_accordance_with_the_rules(
            decimal? price,
            IReadOnlyCollection<Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants.QuantityQuality> qualities,
            ChargeType? chargeType,
            CalculatedQuantityQuality expectedCalculatedQuantityQuality)
        {
            // Act
            var actual = QuantityQualitiesFactor.CreateQuantityQuality(
                price,
                qualities,
                chargeType);

            // Assert
            actual.Should().Be(expectedCalculatedQuantityQuality);
        }
    }
}
