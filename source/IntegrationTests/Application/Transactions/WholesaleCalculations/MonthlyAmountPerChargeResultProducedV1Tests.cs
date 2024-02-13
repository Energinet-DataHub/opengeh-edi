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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleCalculations;

public class MonthlyAmountPerChargeResultProducedV1Tests : TestBase
{
    private readonly IIntegrationEventHandler _integrationEventHandler;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IFileStorageClient _fileStorageClient;

    private readonly MonthlyAmountPerChargeResultProducedV1EventBuilder _monthlyPerChargeEventBuilder = new();

    public MonthlyAmountPerChargeResultProducedV1Tests(
        IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _integrationEventHandler = GetService<IIntegrationEventHandler>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    [Fact]
    public async Task MonthlyAmountPerChargeResultProducedV1Processor_creates_outgoing_message_when_feature_is_enabled()
    {
        var monthlyPerChargeEvent = _monthlyPerChargeEventBuilder.Build();
        await HandleIntegrationEventAsync(monthlyPerChargeEvent);
        await AssertOutgoingMessageAsync();
    }

    [Fact]
    public async Task MonthlyAmountPerChargeResultProducedV1Processor_does_not_create_outgoing_message_when_feature_is_disabled()
    {
        var monthlyPerChargeEvent = _monthlyPerChargeEventBuilder
            .WithCalculationType(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
            .Build();

        FeatureFlagManagerStub.UseMonthlyAmountPerChargeResultProduced = Task.FromResult(false);

        await HandleIntegrationEventAsync(monthlyPerChargeEvent);
        await AssertOutgoingMessageIsNull(BusinessReason.WholesaleFixing);
    }

    [Fact]
    public async Task MonthlyAmountPerChargeResultProducedV1Processor_creates_outgoingMessage()
    {
        var startOfPeriod = Instant.FromUtc(2023, 1, 1, 0, 0);
        var endOfPeriod = Instant.FromUtc(2023, 1, 1, 0, 0);
        var energySupplier = "8200000007743";
        var gridAreaCode = "805";
        var chargeCode = "IDontKow";
        var chargeOwner = "9876543216543";
        var isTax = false;
        var amount = new DecimalValue { Units = 100, Nanos = 0 };

        // Arrange
        var monthlyPerChargeEvent = _monthlyPerChargeEventBuilder
            .WithCalculationType(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
            .WithStartOfPeriod(startOfPeriod.ToTimestamp())
            .WithEndOfPeriod(endOfPeriod.ToTimestamp())
            .WithGridAreaCode(gridAreaCode)
            .WithEnergySupplier(energySupplier)
            .WithChargeCode(chargeCode)
            .WithChargeType(MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Fee)
            .WithChargeOwner(chargeOwner)
            .WithQuantityUnit(MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh)
            .WithIsTax(isTax)
            .WithCurrency(MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk)
            .WithAmount(amount)
            .Build();

        // Act
        await HandleIntegrationEventAsync(monthlyPerChargeEvent);

        // Assert
        var message = await AssertOutgoingMessageAsync(businessReason: BusinessReason.WholesaleFixing);

        message
            .HasReceiverId(energySupplier)
            .HasReceiverRole(ActorRole.EnergySupplier.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.CalculationVersion, 1)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.GridAreaCode, gridAreaCode)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.ChargeCode, chargeCode)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.IsTax, isTax)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Quantity, amount.Units + (amount.Nanos / 1_000_000_000))
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.EnergySupplier, ActorNumber.Create(energySupplier))
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.ChargeOwner, ActorNumber.Create(chargeOwner))
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Period.Start, startOfPeriod)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Period.End, endOfPeriod)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.SettlementVersion, null)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.QuantityUnit, MeasurementUnit.Kwh)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.PriceMeasureUnit, MeasurementUnit.Kwh)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Currency, Currency.DanishCrowns)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.ChargeType, ChargeType.Fee)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Resolution, Resolution.Monthly);
    }

    [Fact]
    public async Task MonthlyAmountPerChargeResultProducedV1Processor_creates_outgoingMessage_with_no_amount_first_correction()
    {
        // Arrange
        var monthlyPerChargeEvent = _monthlyPerChargeEventBuilder
            .WithCalculationType(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.FirstCorrectionSettlement)
            .WithAmount(null)
            .Build();

        // Act
        await HandleIntegrationEventAsync(monthlyPerChargeEvent);

        // Assert
        var message = await AssertOutgoingMessageAsync(businessReason: BusinessReason.Correction);
        message
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.Quantity, null)
            .HasMessageRecordValue<WholesaleCalculationSeries>(wholesaleCalculation => wholesaleCalculation.SettlementVersion, SettlementVersion.FirstCorrection);
    }

    private async Task HandleIntegrationEventAsync(MonthlyAmountPerChargeResultProducedV1 @event)
    {
        var integrationEvent = new IntegrationEvent(
            Guid.NewGuid(),
            @event.GetType().Name,
            1,
            @event);
        await _integrationEventHandler.HandleAsync(integrationEvent);
    }

    private async Task<AssertOutgoingMessage> AssertOutgoingMessageAsync(
        ActorRole? receiverRole = null,
        BusinessReason? businessReason = null)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyWholesaleServices.Name,
            businessReason?.Name ?? BusinessReason.WholesaleFixing.Name,
            receiverRole ?? ActorRole.EnergySupplier,
            _databaseConnectionFactory,
            _fileStorageClient);
    }

    private async Task AssertOutgoingMessageIsNull(BusinessReason? businessReason = null)
    {
        await AssertOutgoingMessage.OutgoingMessageIsNullAsync(
            messageType: DocumentType.NotifyWholesaleServices.Name,
            businessReason: businessReason?.Name ?? BusinessReason.WholesaleFixing.Name,
            ActorRole.EnergySupplier,
            _databaseConnectionFactory);
    }
}
