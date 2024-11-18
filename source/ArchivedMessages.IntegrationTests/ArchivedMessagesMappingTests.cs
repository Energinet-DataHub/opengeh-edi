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

using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Validation;
using Xunit;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

public class ArchivedMessagesMappingTests
{
    private enum MismatchedEnum
    {
        A = 1,
        B = 2,
        D = 4,
    }

    [Fact]
    public void Given_EnumsAreLogicallyCompatible_When_Mapping_Then_ReturnTrueWhenEnumsMatch()
    {
        // Act
        var result = EnumCompatibilityChecker.AreEnumsCompatible<ArchivedMessageType, ArchivedMessageTypeDto>();

        // Assert
        Assert.True(result, "The enums should be logically compatible.");
    }

    [Fact]
    public void Given_EnumsAreNotLogicallyCompatible_When_Mapping_Then_ReturnFalseWhenEnumsDoNotMatch()
    {
        // Act
        var result = EnumCompatibilityChecker.AreEnumsCompatible<ArchivedMessageType, MismatchedEnum>();

        // Assert
        Assert.False(result, "The enums should not be logically compatible.");
    }
}
