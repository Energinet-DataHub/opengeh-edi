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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain;

public class ExternalIdTests
{
    [Fact]
    public void Given_ManyDifferentInputs_When_HashValuesWithMaxLength_Then_ValuesAreStillUnique()
    {
        // Create 10 almost equal metering point ids (0000000000001, 0000000000002, etc.)
        var meteringPointIds = Enumerable.Range(0, 10)
            .Select(i => i.ToString().PadLeft(totalWidth: 13, paddingChar: '0'));

        // Create 100.000 orchestration instance ids (guids)
        var orchestrationInstanceIds = Enumerable.Range(0, 100000)
            .Select(_ => Guid.NewGuid());

        // Create external ids from each orchestration instance id and metering point id
        var externalIds = orchestrationInstanceIds.SelectMany(
            orchestrationInstanceId => meteringPointIds.Select(
                meteringPointId => ExternalId.HashValuesWithMaxLength(
                        orchestrationInstanceId.ToString("N"),
                        meteringPointId)
                    .Value));

        // Ensure no values are equal
        Assert.Distinct(externalIds.ToList());
    }

    [Fact]
    public void Given_ManyEqualInputs_When_HashValuesWithMaxLength_Then_ValuesAreStillEqual()
    {
        const string meteringPointId = "1234567890123";
        var orchestrationInstanceId = Guid.NewGuid();

        // Create a million external ids from the same orchestration instance id and metering point id
        var externalIds = Enumerable.Range(0, 1000000)
            .Select(_ => ExternalId.HashValuesWithMaxLength(orchestrationInstanceId.ToString("N"), meteringPointId).Value)
            .ToList();

        // Ensure all values are equal
        Assert.All(externalIds, item => Assert.Equal(externalIds.First(), item));
    }
}
