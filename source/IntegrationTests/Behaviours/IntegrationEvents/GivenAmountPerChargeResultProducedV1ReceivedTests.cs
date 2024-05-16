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

using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenAmountPerChargeResultProducedV1ReceivedTests : WholesaleServicesBehaviourTestBase
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
    public async Task When_ChargeIsNotTaxAndEnergySupplierAndChargeOwnerActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocuments(DocumentFormat documentFormat)
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
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);
        var peekResultsForChargeOwner = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933444"), ActorRole.GridOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var peekResultForChargeOwner = peekResultsForChargeOwner.Should().ContainSingle().Subject;

        var assertionInputForEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5799999933318",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
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
                    Price = DecimalValue.FromDecimal(10),
                    Quantity = DecimalValue.FromDecimal(3),
                    Amount = DecimalValue.FromDecimal(30),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        var assertionInputForChargeOwner = assertionInputForEnergySupplier with
        {
            ReceiverId = "5799999933444",
            ReceiverRole = ActorRole.GridOperator,
        };

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            assertionInputForEnergySupplier);
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForChargeOwner.Bundle,
            documentFormat,
            assertionInputForChargeOwner);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsTaxAndEnergySupplierAndGridOwnerActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var gridOwnerActorNumber = ActorNumber.Create("5799999933555");
        await GivenGridAreaOwnershipAsync("244", gridOwnerActorNumber);
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
                .WithIsTax(true));

        await GivenIntegrationEventReceived(amountPerChargeResultProducedEvent);

        // Act
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(ActorNumber.Create(gridOwnerActorNumber.Value), ActorRole.GridOperator, documentFormat);
        var peekResultsForChargeOwner = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933444"), ActorRole.GridOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var peekResultForGridOperator = peekResultsForGridOperator.Should().ContainSingle().Subject;
        peekResultsForChargeOwner.Should().BeEmpty();

        var assertionInputForEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5799999933318",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
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
                    Price = DecimalValue.FromDecimal(10),
                    Quantity = DecimalValue.FromDecimal(3),
                    Amount = DecimalValue.FromDecimal(30),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        var assertionInputForGridOperator = assertionInputForEnergySupplier with
        {
            ReceiverId = gridOwnerActorNumber.Value,
            ReceiverRole = ActorRole.GridOperator,
        };

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            assertionInputForEnergySupplier);
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForGridOperator.Bundle,
            documentFormat,
            assertionInputForGridOperator);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsNotTaxAndChargeOwnerIsSystemOperatorAndEnergySupplierAndSystemOperatorActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocuments(
        DocumentFormat documentFormat)
    {
        // receiver role = EZ
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var chargeOwner = DataHubDetails.SystemOperatorActorNumber;
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always provide each property.
        var amountPerChargeResultProducedEvent = GivenAmountPerChargeResultProducedV1Event(
            @event => @event
                .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
                .WithChargeOwner(chargeOwner.Value)
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
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);
        var peekResultsForChargeOwner = await WhenActorPeeksAllMessages(ActorNumber.Create(chargeOwner.Value), ActorRole.SystemOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var peekResultForChargeOwner = peekResultsForChargeOwner.Should().ContainSingle().Subject;

        var assertionInputForEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5799999933318",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: DataHubDetails.SystemOperatorActorNumber.Value,
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
                    Price = DecimalValue.FromDecimal(10),
                    Quantity = DecimalValue.FromDecimal(3),
                    Amount = DecimalValue.FromDecimal(30),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        var assertionInputForChargeOwner = assertionInputForEnergySupplier with
        {
            ReceiverId = chargeOwner.Value,
            ReceiverRole = ActorRole.SystemOperator,
        };

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            assertionInputForEnergySupplier);
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForChargeOwner.Bundle,
            documentFormat,
            assertionInputForChargeOwner);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsTaxAndChargeOwnerIsSystemOperatorAndEnergySupplierAndGridOwnerActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocuments(
        DocumentFormat documentFormat)
    {
        // receiver role = EZ
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var gridOwnerActorNumber = ActorNumber.Create("5799999933555");
        await GivenGridAreaOwnershipAsync("244", gridOwnerActorNumber);
        var chargeOwner = DataHubDetails.SystemOperatorActorNumber;
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always provide each property.
        var amountPerChargeResultProducedEvent = GivenAmountPerChargeResultProducedV1Event(
            @event => @event
                .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
                .WithChargeOwner(chargeOwner.Value)
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
                .WithIsTax(true));

        await GivenIntegrationEventReceived(amountPerChargeResultProducedEvent);

        // Act
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(ActorNumber.Create(gridOwnerActorNumber.Value), ActorRole.GridOperator, documentFormat);
        var peekResultsForChargeOwner = await WhenActorPeeksAllMessages(ActorNumber.Create(chargeOwner.Value), ActorRole.SystemOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var peekResultForGridOperator = peekResultsForGridOperator.Should().ContainSingle().Subject;
        peekResultsForChargeOwner.Should().BeEmpty();

        var assertionInputForEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5799999933318",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: DataHubDetails.SystemOperatorActorNumber.Value,
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
                    Price = DecimalValue.FromDecimal(10),
                    Quantity = DecimalValue.FromDecimal(3),
                    Amount = DecimalValue.FromDecimal(30),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        var assertionInputForGridOperator = assertionInputForEnergySupplier with
        {
            ReceiverId = gridOwnerActorNumber.Value,
            ReceiverRole = ActorRole.GridOperator,
        };

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            assertionInputForEnergySupplier);
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForGridOperator.Bundle,
            documentFormat,
            assertionInputForGridOperator);
    }
}
