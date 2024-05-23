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
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenTotalMonthlyAmountResultProducedV1ReceivedTests : WholesaleServicesBehaviourTestBase
{
    public GivenTotalMonthlyAmountResultProducedV1ReceivedTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(
            integrationTestFixture,
            testOutputHelper)
    {
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_ChargeOwnerActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        // Arrange
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        var chargeOwnerId = "5790002241111";
        var totalMonthlyAmountResultProducedEvent = GivenTotalMonthlyAmountResultProducedV1Event(
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing,
            periodStart: CreateDateInstant(2023, 12, 31),
            periodEnd: CreateDateInstant(2024, 01, 31),
            gridAreaCode: "740",
            energySupplierId: "5790002243172",
            chargeOwnerId: chargeOwnerId,
            new DecimalValue()
            {
                Nanos = 8888, Units = 8888,
            });

        await GivenIntegrationEventReceived(totalMonthlyAmountResultProducedEvent);

        // Act
        var peekResultsAsChargeOwner = await WhenActorPeeksAllMessages(ActorNumber.Create(chargeOwnerId), ActorRole.GridOperator, documentFormat);

        // Assert
        var peekResultAsChargeOwner = peekResultsAsChargeOwner.Should().ContainSingle().Subject;
        var expectedDocumentToChargeOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5790002241111",
            ReceiverRole: ActorRole.GridOperator,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null, //ChargeOwner is not writting in the document for total sum
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: "5790002243172",
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "740",
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: 1,
            Resolution: Resolution.Monthly,
            Period: new Period(CreateDateInstant(2023, 12, 31), CreateDateInstant(2024, 01, 31)),
            Points: new[]
            {
                new WholesaleServicesRequestSeries.Types.Point
                {
                    Price = null,
                    Quantity = null,
                    Amount = Energinet.DataHub.Edi.Responses.DecimalValue.FromDecimal(8888.000008888M),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultAsChargeOwner.Bundle,
            documentFormat,
            expectedDocumentToChargeOwner);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_EnergySupplierActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        // Arrange
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        var energySupplierId = "5790002243172";
        var totalMonthlyAmountResultProducedEvent = GivenTotalMonthlyAmountResultProducedV1Event(
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing,
            periodStart: CreateDateInstant(2023, 12, 31),
            periodEnd: CreateDateInstant(2024, 01, 31),
            gridAreaCode: "740",
            energySupplierId: energySupplierId,
            chargeOwnerId: null,
            new DecimalValue()
            {
                Nanos = 8888, Units = 8888,
            });

        await GivenIntegrationEventReceived(totalMonthlyAmountResultProducedEvent);

        // Act
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create(energySupplierId), ActorRole.EnergySupplier, documentFormat);

        // Assert
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var expectedDocumentToEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5790002243172",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null, //ChargeOwner is not allowed in the document for total sum
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: "5790002243172",
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "740",
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: 1,
            Resolution: Resolution.Monthly,
            Period: new Period(CreateDateInstant(2023, 12, 31), CreateDateInstant(2024, 01, 31)),
            Points: new[]
            {
                new WholesaleServicesRequestSeries.Types.Point
                {
                    Price = null,
                    Quantity = null,
                    Amount = Energinet.DataHub.Edi.Responses.DecimalValue.FromDecimal(8888.000008888M),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task Given_MissingAmount_When_EnergySupplierActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        // Arrange
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));

        var energySupplierId = "5790002243172";
        var totalMonthlyAmountResultProducedEvent = GivenTotalMonthlyAmountResultProducedV1Event(
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing,
            periodStart: CreateDateInstant(2023, 12, 31),
            periodEnd: CreateDateInstant(2024, 01, 31),
            gridAreaCode: "740",
            energySupplierId: energySupplierId,
            chargeOwnerId: null,
            amount: null);

        await GivenIntegrationEventReceived(totalMonthlyAmountResultProducedEvent);

        // Act
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(ActorNumber.Create(energySupplierId), ActorRole.EnergySupplier, documentFormat);

        // Assert
        var peekResultForEnergySupplier = peekResultsForEnergySupplier.Should().ContainSingle().Subject;
        var expectedDocumentToEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5790002243172",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null, //ChargeOwner is not allowed in the document for total sum
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: "5790002243172",
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "740",
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: 1,
            Resolution: Resolution.Monthly,
            Period: new Period(CreateDateInstant(2023, 12, 31), CreateDateInstant(2024, 01, 31)),
            Points: new[]
            {
                new WholesaleServicesRequestSeries.Types.Point
                {
                    Price = null,
                    Quantity = null,
                    Amount = Energinet.DataHub.Edi.Responses.DecimalValue.FromDecimal(0),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    private TotalMonthlyAmountResultProducedV1 GivenTotalMonthlyAmountResultProducedV1Event(
        TotalMonthlyAmountResultProducedV1.Types.CalculationType calculationType,
        Instant periodStart,
        Instant periodEnd,
        string gridAreaCode,
        string energySupplierId,
        string? chargeOwnerId,
        DecimalValue? amount = null)
    {
        var totalMonthlyAmountResultProducedV1Event = new TotalMonthlyAmountResultProducedV1()
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = calculationType,
            PeriodStartUtc = periodStart.ToTimestamp(),
            PeriodEndUtc = periodEnd.ToTimestamp(),
            GridAreaCode = gridAreaCode,
            EnergySupplierId = energySupplierId,
            Currency = TotalMonthlyAmountResultProducedV1.Types.Currency.Dkk,
            CalculationResultVersion = 1,
        };

        if (amount != null)
        {
            totalMonthlyAmountResultProducedV1Event.Amount = amount;
        }

        if (chargeOwnerId is not null)
            totalMonthlyAmountResultProducedV1Event.ChargeOwnerId = chargeOwnerId;

        return totalMonthlyAmountResultProducedV1Event;
    }
}
