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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenAmountPerChargeResultProducedV1ReceivedTests : BehavioursTestBase
{
    public GivenAmountPerChargeResultProducedV1ReceivedTests(IntegrationTestFixture fixture)
        : base(fixture)
    {
    }

    public static object[][] AllDocumentFormats()
    {
        return EnumerationType.GetAll<DocumentFormat>()
            .Select(df => new object[] { df })
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(AllDocumentFormats))]
    public async Task When_EnergySupplierPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(DocumentFormat documentFormat)
    {
        // Arrange
        var amountPerChargeEvent = new AmountPerChargeResultProducedV1EventBuilder()
            .WithEnergySupplier("1111111111111")
            .Build();

        await GivenIntegrationEventReceived(amountPerChargeEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier, documentFormat);

        // Assert
        await ThenDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            (document) => document
                .HasReceiver("1111111111111")
                .HasEnergySupplier("1111111111111"));
    }
}
