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
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.Models;

public class ActorNumberTests
{
    public static TheoryData<string> GetInvalidGlnNumbers() =>
        new TheoryData<string>()
        {
            "12345678901234", // 14 digits
            "123456789012a",  // 12 digits + 1 letter
        };

    public static TheoryData<string> GetValidEicCodes() =>
        new TheoryData<string>()
        {
            // https://www.entsoe.eu/data/energy-identification-codes-eic/code-generator/eic_key_generator
            "123456789012345P",
            "10X123456789012L",
            "10X------------J",
            "14X---------1238",
        };

    [Fact]
    public void Given_ValidGlnNumber_When_IsGlnNumber_Then_ReturnTrue()
    {
        // Arrange
        const string actorNumber = "5790001234567";

        // Act
        var result = ActorNumber.IsGlnNumber(actorNumber);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(GetInvalidGlnNumbers))]
    public void Given_InvalidGlnNumbers_When_IsGlnNumber_Then_ReturnFalse(string actorNumber)
    {
        // Act
        var result = ActorNumber.IsGlnNumber(actorNumber);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(GetValidEicCodes))]
    public void Given_ValidEicCode_When_IsEic_Then_ReturnTrue(string actorNumber)
    {
        // Act
        var result = ActorNumber.IsEic(actorNumber);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Given_InValidEicNumber_When_IsEic_Then_ReturnFalse()
    {
        // Arrange
        const string actorNumber = "10X12345678901234"; // Length 17

        // Act
        var result = ActorNumber.IsEic(actorNumber);

        // Assert
        Assert.False(result);
    }
}
