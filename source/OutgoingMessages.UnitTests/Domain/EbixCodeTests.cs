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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.MarketDocuments;

public sealed class EbixCodeTests
{
    public static IEnumerable<object[]> EdiQualityValues()
    {
        return Enum.GetValues<CalculatedQuantityQuality>().Select(e => new[] { (object)e });
    }

    public static IEnumerable<object?[]> ExpectedCalculatedQuantityQualityEbixCodeValues()
    {
        return new[]
        {
            new object?[] { CalculatedQuantityQuality.NotAvailable, null },
            new object[] { CalculatedQuantityQuality.Estimated, EbixCode.QuantityQualityCodeEstimated },
            new object[] { CalculatedQuantityQuality.Measured, EbixCode.QuantityQualityCodeMeasured },
            new object?[] { CalculatedQuantityQuality.Missing, null },
            new object[] { CalculatedQuantityQuality.Incomplete, EbixCode.QuantityQualityCodeEstimated },
            new object[] { CalculatedQuantityQuality.Calculated, EbixCode.QuantityQualityCodeMeasured },
        };
    }

    [Theory]
    [MemberData(nameof(ExpectedCalculatedQuantityQualityEbixCodeValues))]
    public void Given_EdiQuality_When_ForEnergyResultOf_Then_CorrectEbixValue(
        CalculatedQuantityQuality quantityQuality,
        string? expectedResult)
    {
        // Arrange & Act
        var result = EbixCode.ForEnergyResultOf(quantityQuality);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(EdiQualityValues))]
    [ExcludeFromNameConventionCheck]
    public void Can_handle_all_edi_qualities(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        // Act & Assert
        EbixCode.ForEnergyResultOf(calculatedQuantityQuality);
    }

    [Fact]
    [ExcludeFromNameConventionCheck]
    public void All_Calculated_Quality_Qualities_are_considered_in_mapping()
    {
        ExpectedCalculatedQuantityQualityEbixCodeValues()
            .SelectMany((cqq, _) => cqq)
            .Should()
            .Contain(EdiQualityValues().SelectMany(cqq => cqq));
    }
}
