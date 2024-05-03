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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Xunit.Abstractions;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenTotalMonthlyAmountResultProducedV1ProcessorTests : BehavioursTestBase
{
    protected GivenTotalMonthlyAmountResultProducedV1ProcessorTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(
            integrationTestFixture,
            testOutputHelper)
    {
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task When_EnergySupplierActorAndChargeOwnerActorPeeksMessage_Then_ReceivesCorrectNotifyWholesaleServicesDocument(
        DocumentFormat documentFormat)
    {
        // Arrange
        var totalMonthlyAmountResultProducedEvent = GivenTotalMonthlyAmountResultProducedV1Event(
            TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing,
            CreateDateInstant(2023, 12, 31),
            CreateDateInstant(2024, 01, 31),
            "740",
            "5790002243172",
            "5790002241111");

        await GivenIntegrationEventReceived(totalMonthlyAmountResultProducedEvent);

        // Act
        var peekResultForEnergySupplier = await WhenActorPeeksMessage(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier, documentFormat);
        var peekResultForChargeOwner = await WhenActorPeeksMessage(ActorNumber.Create("5799999933318"), ActorRole.GridOperator, documentFormat);

        // Assert
        var expectedDocumentToEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5790002243172",
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: "5790001330552",
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: "5790002243172",
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "740",
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: null,
            CalculationVersion: 1,
            Resolution: null,
            Period: new Period(CreateDateInstant(2023, 12, 31), CreateDateInstant(2024, 01, 31)),
            Points: new[]
            {
                new WholesaleServicesRequestSeries.Types.Point
                {
                    Price = null,
                    Quantity = null,
                    Amount = Energinet.DataHub.Edi.Responses.DecimalValue.FromDecimal(8888888),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        var expectedDocumentToChargeOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: "5790002241111",
            ReceiverRole: ActorRole.GridOperator,
            SenderId: "5790001330552",
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: "5790002243172",
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "740",
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: null,
            CalculationVersion: 1,
            Resolution: null,
            Period: new Period(CreateDateInstant(2023, 12, 31), CreateDateInstant(2024, 01, 31)),
            Points: new[]
            {
                new WholesaleServicesRequestSeries.Types.Point
                {
                    Price = null,
                    Quantity = null,
                    Amount = Energinet.DataHub.Edi.Responses.DecimalValue.FromDecimal(8888888),
                    QuantityQualities = { QuantityQuality.Calculated },
                },
            });

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForEnergySupplier.Bundle,
            documentFormat,
            expectedDocumentToEnergySupplier);
        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResultForChargeOwner.Bundle,
            documentFormat,
            expectedDocumentToChargeOwner);
    }

    private TotalMonthlyAmountResultProducedV1 GivenTotalMonthlyAmountResultProducedV1Event(
        TotalMonthlyAmountResultProducedV1.Types.CalculationType calculationType,
        Instant periodStart,
        Instant periodEnd,
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId)
    {
        return new TotalMonthlyAmountResultProducedV1()
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = calculationType,
            PeriodStartUtc = periodStart.ToTimestamp(),
            PeriodEndUtc = periodEnd.ToTimestamp(),
            GridAreaCode = gridAreaCode,
            EnergySupplierId = energySupplierId,
            Currency = TotalMonthlyAmountResultProducedV1.Types.Currency.Dkk,
            ChargeOwnerId = chargeOwnerId,
            Amount = new DecimalValue()
            {
                Nanos = 8888, Units = 8888,
            },
            // CalculationResultVersion = 1, TODO: Set when contract is updated.
        };
    }
}
