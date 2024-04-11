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

using System;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
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
    public async Task When_EnergySupplierActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(DocumentFormat documentFormat)
    {
        // Arrange
        var amountPerChargeResultProducedEvent = GivenAmountPerChargeResultProducedV1Event(
            @event => @event
                .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
                .WithStartOfPeriod(CreateDateInstant(2024, 1, 1).ToTimestamp())
                .WithEndOfPeriod(CreateDateInstant(2024, 1, 31).ToTimestamp())
                .WithGridAreaCode("100")
                .WithEnergySupplier("1111111111111")
                .WithChargeCode("222")
                .WithChargeType(AmountPerChargeResultProducedV1.Types.ChargeType.Tariff)
                .WithChargeOwner("333")
                .WithResolution(AmountPerChargeResultProducedV1.Types.Resolution.Hour)
                .WithQuantityUnit(AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh)
                .WithMeteringPointType(AmountPerChargeResultProducedV1.Types.MeteringPointType.Consumption)
                .WithSettlementMethod(AmountPerChargeResultProducedV1.Types.SettlementMethod.Flex)
                .WithIsTax(false)
                .WithCurrency(AmountPerChargeResultProducedV1.Types.Currency.Dkk)
                .WithCalculationVersion(1));

        await GivenIntegrationEventReceived(amountPerChargeResultProducedEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier, documentFormat);

        // Assert
        await ThenDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            document => document
                .HasReceiverId(ActorNumber.Create("1111111111111"))
                .HasReceiverRole(ActorRole.EnergySupplier, CodeListType.Ebix)
                .HasEnergySupplierNumber(ActorNumber.Create("1111111111111"), "A10")
                .HasPositionAndQuantity());
    }
}
