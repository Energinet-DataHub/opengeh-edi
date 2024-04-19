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
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenAmountPerChargeResultProducedV1ReceivedTests : BehavioursTestBase
{
    public GivenAmountPerChargeResultProducedV1ReceivedTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
    }

#pragma warning disable CS1570 // XML comment has badly formed XML
    /// <summary>
    /// Example based on: https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=039914efcff74ec1a159eff3bb358f68&e=0RN8ma
    /// </summary>
    /// <param name="documentFormat"></param>
#pragma warning restore CS1570 // XML comment has badly formed XML
    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_EnergySupplierActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(DocumentFormat documentFormat)
    {
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always provide each property.
        var amountPerChargeResultProducedEvent = GivenAmountPerChargeResultProducedV1Event(
            @event => @event
                .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
                .WithChargeOwner("5799999933444")
                .WithChargeCode("25361478")
                .WithChargeType(AmountPerChargeResultProducedV1.Types.ChargeType.Tariff)
                .WithCurrency(AmountPerChargeResultProducedV1.Types.Currency.Dkk)
                .WithEnergySupplier(
                    "5799999933318") // Example says 5790001330552, but isn't this the sender, not the energy supplier?
                .WithSettlementMethod(AmountPerChargeResultProducedV1.Types.SettlementMethod.Flex)
                .WithMeteringPointType(AmountPerChargeResultProducedV1.Types.MeteringPointType.Consumption)
                .WithGridAreaCode("244")
                .WithQuantityUnit(AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh)
                .WithCalculationVersion(1)
                .WithResolution(AmountPerChargeResultProducedV1.Types.Resolution.Hour)
                .WithStartOfPeriod(CreateDateInstant(2022, 09, 06).ToTimestamp())
                .WithEndOfPeriod(CreateDateInstant(2022, 09, 07).ToTimestamp())
                .WithPoint(1, 10, 3, 30, AmountPerChargeResultProducedV1.Types.QuantityQuality.Calculated) // QuantityQualityCodeCalculated = "A06". "A01" from example doesn't exist?
                .WithIsTax(false));

        await GivenIntegrationEventReceived(amountPerChargeResultProducedEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2022-09-07T13:37:05Z",
                BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
                ReceiverId: "5799999933318",
                ReceiverRole: ActorRole.EnergySupplier,
                SenderId: "5790001330552",
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: "5799999933444",
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: "5799999933318",
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "244",
                OriginalTransactionIdReference: null,
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: 1,
                Resolution: Resolution.Hourly,
                Period: new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)),
                Points: new[]
                {
                    new WholesaleServicesRequestSeries.Types.Point
                    {
                        Price = new DecimalValue { Units = 10, Nanos = 0 },
                        Quantity = new DecimalValue { Units = 3, Nanos = 0 },
                        Amount = new DecimalValue { Units = 30, Nanos = 0 },
                        QuantityQualities = { QuantityQuality.Calculated },
                    },
                }));
            // ));
            //     document => document
            //         // -- Assert header values --
            //         .MessageIdExists()
            //         // Assert businessSector.type? (23)
            //         .HasTimestamp()
            //         .HasBusinessReason(BusinessReason.WholesaleFixing, CodeListType.EbixDenmark)
            //         .HasReceiverId(ActorNumber.Create())
            //         .HasReceiverRole(ActorRole.EnergySupplier, CodeListType.Ebix)
            //         .HasSenderId(ActorNumber.Create("5790001330552"), "A10") // Sender is DataHub
            //         .HasSenderRole(ActorRole.MeteredDataAdministrator)
            //         // Assert type? (E31)
            //         // -- Assert series values --
            //         .TransactionIdExists()
            //         .HasChargeTypeOwner(ActorNumber.Create("5799999933444"), "A10")
            //         .HasChargeCode("25361478")
            //         .HasChargeType(ChargeType.Tariff)
            //         .HasCurrency(Currency.DanishCrowns)
            //         .HasEnergySupplierNumber(ActorNumber.Create("5799999933318"), "A10")
            //         .HasSettlementMethod(SettlementMethod.Flex)
            //         .HasMeteringPointType(MeteringPointType.Consumption)
            //         .HasGridAreaCode("244", "NDK")
            //         .OriginalTransactionIdReferenceDoesNotExist()
            //         .HasPriceMeasurementUnit(MeasurementUnit.Kwh)
            //         .HasProductCode("5790001330590") // Example says "8716867000030", but document writes as "5790001330590"?
            //         .HasQuantityMeasurementUnit(MeasurementUnit.Kwh)
            //         .SettlementVersionDoesNotExist()
            //         .HasCalculationVersion(1)
            //         .HasResolution(Resolution.Hourly)
            //         .HasPeriod(new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)))
            //         .HasSumQuantityForPosition(1, 30)
            //         .HasQuantityForPosition(1, 3)
            //         .HasPriceForPosition(1, "10")
            //         .HasQualityForPosition(1, CalculatedQuantityQuality.Calculated));
    }
}
