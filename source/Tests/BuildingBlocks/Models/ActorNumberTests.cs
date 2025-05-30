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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.Models;

public class ActorNumberTests
{
    public static TheoryData<string> GetInvalidActorNumbers() =>
        new TheoryData<string>()
        {
            "12345678901234", // 14 digits
            "123456789012a",  // 12 digits + 1 letter
            "10X-----------J", // 15 characters
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
        var result = ActorNumber.Create(actorNumber);

        // Assert
        Assert.True(result.IsGlnNumber());
        Assert.False(result.IsEic());
    }

    [Theory]
    [MemberData(nameof(GetValidEicCodes))]
    public void Given_ValidEicCode_When_IsEic_Then_ReturnTrue(string actorNumber)
    {
        // Act
        var result = ActorNumber.Create(actorNumber);

        // Assert
        Assert.False(result.IsGlnNumber());
        Assert.True(result.IsEic());
    }

    [Theory]
    [MemberData(nameof(GetInvalidActorNumbers))]
    public void Given_InvalidActorNumber_When_Create_Then_Throws(string actorNumber)
    {
        // Act
        var createActor = () => ActorNumber.Create(actorNumber);

        // Assert
        createActor.Should().ThrowExactly<InvalidActorNumberException>();
    }
}
