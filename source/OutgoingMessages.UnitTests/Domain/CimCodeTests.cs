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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.MarketDocuments;

public sealed class CimCodeTests
{
    public static IEnumerable<object[]> CalculatedQualityValues()
    {
        return Enum.GetValues<CalculatedQuantityQuality>().Select(e => new object[] { e });
    }

    public static IEnumerable<object[]> ExpectedCalculatedQuantityQualityCimCodeValues()
    {
        return new[]
        {
            new object[] { CalculatedQuantityQuality.NotAvailable, CimCode.QuantityQualityCodeNotAvailable },
            new object[] { CalculatedQuantityQuality.Estimated, CimCode.QuantityQualityCodeEstimated },
            new object[] { CalculatedQuantityQuality.Measured, CimCode.QuantityQualityCodeMeasured },
            new object[] { CalculatedQuantityQuality.Missing, CimCode.QuantityQualityCodeIncomplete },
            new object[] { CalculatedQuantityQuality.Incomplete, CimCode.QuantityQualityCodeIncomplete },
            new object[] { CalculatedQuantityQuality.Calculated, CimCode.QuantityQualityCodeCalculated },
        };
    }

    [Theory]
    [MemberData(nameof(ExpectedCalculatedQuantityQualityCimCodeValues))]
    public void Given_EdiQuality_When_ForEnergyResultOf_Then_CorrectCimValue(
        CalculatedQuantityQuality calculatedQuantityQuality,
        string expectedCimCode)
    {
        var cimQuality = CimCode.ForEnergyResultOf(calculatedQuantityQuality);

        cimQuality.Should().Be(expectedCimCode);
    }

    [Theory]
    [MemberData(nameof(CalculatedQualityValues))]
    [ExcludeFromNameConventionCheck]
    public void Can_handle_all_edi_qualities(CalculatedQuantityQuality calculatedQuantityQuality)
    {
        // Act
        CimCode.ForEnergyResultOf(calculatedQuantityQuality);
    }

    [Fact]
    [ExcludeFromNameConventionCheck]
    public void All_Calculated_Quality_Qualities_are_considered_in_mapping()
    {
        ExpectedCalculatedQuantityQualityCimCodeValues()
            .SelectMany((cqq, _) => cqq)
            .Should()
            .Contain(CalculatedQualityValues().SelectMany(cqq => cqq));
    }
}
