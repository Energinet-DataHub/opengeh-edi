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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenMonthlyAmountPerChargeResultProducedV1ReceivedTests : WholesaleServicesBehaviourTestBase
{
    public GivenMonthlyAmountPerChargeResultProducedV1ReceivedTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
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
        var monthlyAmountPerChargeResultProducedEvent = GivenMonthlyAmountPerChargeResultProducedV1Event(
            periodStart: CreateDateInstant(2022, 09, 06),
            periodEnd: CreateDateInstant(2022, 09, 07),
            gridAreaCode: "244",
            energySupplierId: "5799999933318",
            chargeOwnerId: "5799999933444",
            chargeCode: "25361478",
            chargeType: MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff,
            isTax: false);

        await GivenIntegrationEventReceived(monthlyAmountPerChargeResultProducedEvent);

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
                SettlementMethod: null,
                MeteringPointType: null,
                GridArea: "244",
                OriginalTransactionIdReference: null,
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: 1,
                Resolution: Resolution.Monthly,
                Period: new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)),
                Points: new[]
                {
                    new WholesaleServicesRequestSeries.Types.Point
                    {
                        Price = null,
                        Quantity = null,
                        Amount = DecimalValue.FromDecimal(30),
                        QuantityQualities = { },
                    },
                }));
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsTax_Then_GridOwnerReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var gridOwnerActorNumber = ActorNumber.Create("5799999933555");
        await GivenGridAreaOwnershipAsync("244", gridOwnerActorNumber);

        var monthlyAmountPerChargeResultProducedEvent = GivenMonthlyAmountPerChargeResultProducedV1Event(
            periodStart: CreateDateInstant(2022, 09, 06),
            periodEnd: CreateDateInstant(2022, 09, 07),
            gridAreaCode: "244",
            energySupplierId: "5799999933318",
            chargeOwnerId: "5799999933444",
            chargeCode: "25361478",
            chargeType: MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff,
            isTax: true);

        await GivenIntegrationEventReceived(monthlyAmountPerChargeResultProducedEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create(gridOwnerActorNumber.Value), ActorRole.GridOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2022-09-07T13:37:05Z",
                BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
                ReceiverId: gridOwnerActorNumber.Value,
                ReceiverRole: ActorRole.GridOperator,
                SenderId: "5790001330552",
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: "5799999933444",
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: "5799999933318",
                SettlementMethod: null,
                MeteringPointType: null,
                GridArea: "244",
                OriginalTransactionIdReference: null,
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: 1,
                Resolution: Resolution.Monthly,
                Period: new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)),
                Points: new[]
                {
                    new WholesaleServicesRequestSeries.Types.Point
                    {
                        Price = null,
                        Quantity = null,
                        Amount = DecimalValue.FromDecimal(30),
                        QuantityQualities = { },
                    },
                }));
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsNotTax_Then_ChargeOwnerReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var chargeOwner = "5799999933444";
        var monthlyAmountPerChargeResultProducedEvent = GivenMonthlyAmountPerChargeResultProducedV1Event(
            periodStart: CreateDateInstant(2022, 09, 06),
            periodEnd: CreateDateInstant(2022, 09, 07),
            gridAreaCode: "244",
            energySupplierId: "5799999933318",
            chargeOwnerId: chargeOwner,
            chargeCode: "25361478",
            chargeType: MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff,
            isTax: false);

        await GivenIntegrationEventReceived(monthlyAmountPerChargeResultProducedEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create(chargeOwner), ActorRole.GridOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2022-09-07T13:37:05Z",
                BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
                ReceiverId: chargeOwner,
                ReceiverRole: ActorRole.GridOperator,
                SenderId: "5790001330552",
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwner,
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: "5799999933318",
                SettlementMethod: null,
                MeteringPointType: null,
                GridArea: "244",
                OriginalTransactionIdReference: null,
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: 1,
                Resolution: Resolution.Monthly,
                Period: new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)),
                Points: new[]
                {
                    new WholesaleServicesRequestSeries.Types.Point
                    {
                        Price = null,
                        Quantity = null,
                        Amount = DecimalValue.FromDecimal(30),
                        QuantityQualities = { },
                    },
                }));
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeIsNotTaxAndChargeOwnerIsSystemOperator_Then_SystemOperatorReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        // receiver role = EZ
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        // Arrange
        var chargeOwner = DataHubDetails.SystemOperatorActorNumber;
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always provide each property.
        var monthlyAmountPerChargeResultProducedEvent = GivenMonthlyAmountPerChargeResultProducedV1Event(
            periodStart: CreateDateInstant(2022, 09, 06),
            periodEnd: CreateDateInstant(2022, 09, 07),
            gridAreaCode: "244",
            energySupplierId: "5799999933318",
            chargeOwnerId: chargeOwner.Value,
            chargeCode: "25361478",
            chargeType: MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff,
            isTax: false);

        await GivenIntegrationEventReceived(monthlyAmountPerChargeResultProducedEvent);

        // Act
        var peekResult = await WhenActorPeeksMessage(ActorNumber.Create(chargeOwner.Value), ActorRole.SystemOperator, documentFormat);

        // Assert
        // -- Maybe this should force a list of properties, instead of using a builder? Then we are forced to always assert each property.
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2022-09-07T13:37:05Z",
                BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
                ReceiverId: chargeOwner.Value,
                ReceiverRole: ActorRole.SystemOperator,
                SenderId: "5790001330552",
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwner.Value,
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: "5799999933318",
                SettlementMethod: null,
                MeteringPointType: null,
                GridArea: "244",
                OriginalTransactionIdReference: null,
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: 1,
                Resolution: Resolution.Monthly,
                Period: new Period(CreateDateInstant(2022, 09, 06), CreateDateInstant(2022, 09, 07)),
                Points: new[]
                {
                    new WholesaleServicesRequestSeries.Types.Point
                    {
                        Price = null,
                        Quantity = null,
                        Amount = DecimalValue.FromDecimal(30),
                        QuantityQualities = { },
                    },
                }));
    }

    private MonthlyAmountPerChargeResultProducedV1 GivenMonthlyAmountPerChargeResultProducedV1Event(
        Instant periodStart,
        Instant periodEnd,
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId,
        string chargeCode,
        MonthlyAmountPerChargeResultProducedV1.Types.ChargeType chargeType,
        bool isTax)
    {
        var monthlyAmountPerChargeResultProducedV1 = new MonthlyAmountPerChargeResultProducedV1()
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing,
            PeriodStartUtc = periodStart.ToTimestamp(),
            PeriodEndUtc = periodEnd.ToTimestamp(),
            GridAreaCode = gridAreaCode,
            ChargeCode = chargeCode,
            ChargeType = chargeType,
            ChargeOwnerId = chargeOwnerId,
            IsTax = isTax,
            QuantityUnit = MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh,
            EnergySupplierId = energySupplierId,
            Currency = MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk,
            Amount = new Wholesale.Contracts.IntegrationEvents.Common.DecimalValue()
            {
                Nanos = 0, Units = 30,
            },
            CalculationResultVersion = 1,
        };

        return monthlyAmountPerChargeResultProducedV1;
    }
}