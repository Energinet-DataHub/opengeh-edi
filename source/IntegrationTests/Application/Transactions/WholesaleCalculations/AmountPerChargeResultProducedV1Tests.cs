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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleCalculations;

public class AmountPerChargeResultProducedV1Tests : TestBase
{
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly IFileStorageClient _fileStorageClient;

    private readonly AmountPerChargeResultProducedV1EventBuilder _amountPerChargeEventBuilder = new();
    private IIntegrationEventHandler _integrationEventHandler;

    public AmountPerChargeResultProducedV1Tests(
        IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _integrationEventHandler = GetService<IIntegrationEventHandler>();
        _databaseConnectionFactory = GetService<IDatabaseConnectionFactory>();
        _fileStorageClient = GetService<IFileStorageClient>();
    }

    public static IEnumerable<object[]> AllMeteringPointTypes()
    {
        var allValues = Enum.GetValues<AmountPerChargeResultProducedV1.Types.MeteringPointType>();

        return allValues
            .Except(new[] { AmountPerChargeResultProducedV1.Types.MeteringPointType.Unspecified })
            .Select(mpt => new object[] { mpt });
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_creates_outgoing_message_when_feature_is_enabled()
    {
        var amountPerChargeEvent = _amountPerChargeEventBuilder.Build();
        await HandleIntegrationEventAsync(amountPerChargeEvent);
        await AssertOutgoingMessageAsync();
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_rollback_both_outgoing_messages_when_a_message_fails()
    {
        var serviceProviderWithCorruptedOutgoingMessagesClient = BuildServiceProviderWithCorruptedOutgoingMessagesClient();
        _integrationEventHandler = serviceProviderWithCorruptedOutgoingMessagesClient.GetRequiredService<IIntegrationEventHandler>();
        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .Build();
        try
        {
            await HandleIntegrationEventAsync(amountPerChargeEvent);
        }
        catch (InvalidDataException)
        {
            // ignored
        }

        await AssertOutgoingMessageIsNull(ActorRole.EnergySupplier);
        await AssertOutgoingMessageIsNull(ActorRole.GridOperator);
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_creates_outgoing_messages_to_energy_supplier_and_charge_owner()
    {
        var amountPerChargeEvent = _amountPerChargeEventBuilder.Build();
        await HandleIntegrationEventAsync(amountPerChargeEvent);
        await AssertOutgoingMessageAsync(ActorRole.EnergySupplier);
        await AssertOutgoingMessageAsync(ActorRole.GridOperator);
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_creates_outgoing_message_to_energy_supplier_and_system_operator_as_charge_owner()
    {
        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithChargeOwner(DataHubDetails.SystemOperatorActorNumber.Value)
            .Build();
        await HandleIntegrationEventAsync(amountPerChargeEvent);
        await AssertOutgoingMessageAsync(ActorRole.EnergySupplier);
        await AssertOutgoingMessageAsync(ActorRole.SystemOperator);
    }

    [Fact]
    public async Task
        AmountPerChargeResultProducedV1Processor_creates_outgoing_messages_to_energy_supplier_and_grid_owner_if_tax_and_charge_owner_is_grid_owner()
    {
        const string gridOperatorAndChargeOwner = "8200000007740";

        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithIsTax(true)
            .WithChargeOwner(gridOperatorAndChargeOwner)
            .Build();

        await new GridAreaBuilder()
            .WithGridAreaCode(amountPerChargeEvent.GridAreaCode)
            .WithActorNumber(ActorNumber.Create(gridOperatorAndChargeOwner))
            .StoreAsync(GetService<IMasterDataClient>());

        await HandleIntegrationEventAsync(amountPerChargeEvent);

        var energySupplierMessage = await AssertOutgoingMessageAsync(ActorRole.EnergySupplier);
        var gridOperatorMessage = await AssertOutgoingMessageAsync(ActorRole.GridOperator);
        var assertSystemOperatorMessage = async () => await AssertOutgoingMessageAsync(ActorRole.SystemOperator);

        energySupplierMessage.HasReceiverId(amountPerChargeEvent.EnergySupplierId);
        gridOperatorMessage.HasReceiverId(gridOperatorAndChargeOwner);
        await assertSystemOperatorMessage.Should()
            .ThrowAsync<XunitException>()
            .WithMessage("Expected object not to be <null>*");
    }

    [Fact]
    public async Task
        AmountPerChargeResultProducedV1Processor_creates_outgoing_messages_to_energy_supplier_and_grid_owner_if_tax_and_charge_owner_is_tso()
    {
        const string gridOperator = "8200000007740";

        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithIsTax(true)
            .WithChargeOwner(DataHubDetails.DataHubActorNumber.Value)
            .Build();

        await new GridAreaBuilder()
            .WithGridAreaCode(amountPerChargeEvent.GridAreaCode)
            .WithActorNumber(ActorNumber.Create(gridOperator))
            .StoreAsync(GetService<IMasterDataClient>());

        await HandleIntegrationEventAsync(amountPerChargeEvent);

        var energySupplierMessage = await AssertOutgoingMessageAsync(ActorRole.EnergySupplier);
        var gridOperatorMessage = await AssertOutgoingMessageAsync(ActorRole.GridOperator);
        var assertSystemOperatorMessage = async () => await AssertOutgoingMessageAsync(ActorRole.SystemOperator);

        energySupplierMessage.HasReceiverId(amountPerChargeEvent.EnergySupplierId);
        gridOperatorMessage.HasReceiverId(gridOperator);
        await assertSystemOperatorMessage.Should()
            .ThrowAsync<XunitException>()
            .WithMessage("Expected object not to be <null>*");
    }

    [Fact]
    public async Task
        AmountPerChargeResultProducedV1Processor_does_not_create_outgoing_messages_if_tax_and_grid_area_is_without_owner()
    {
        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithIsTax(true)
            .WithChargeOwner(DataHubDetails.DataHubActorNumber.Value)
            .Build();

        // This shouldn't really happen, but now it is documented
        var act = async () => await HandleIntegrationEventAsync(amountPerChargeEvent);

        await act.Should()
            .ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("No owner found for grid area code: *");
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_does_not_create_outgoing_message_when_feature_is_disabled()
    {
        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
            .Build();

        FeatureFlagManagerStub.EnableAmountPerChargeResultProduced(false);

        await HandleIntegrationEventAsync(amountPerChargeEvent);
        await AssertOutgoingMessageIsNull(businessReason: BusinessReason.WholesaleFixing);
    }

    [Fact]
    public async Task AmountPerChargeResultProducedV1Processor_creates_outgoingMessage_with_expected_values()
    {
        var startOfPeriod = Instant.FromUtc(2023, 1, 1, 0, 0);
        var endOfPeriod = Instant.FromUtc(2023, 1, 1, 0, 0);
        var energySupplier = "8200000007743";
        var gridAreaCode = "805";
        var chargeCode = "ESP-C-F-04";
        var chargeOwner = "9876543216543";
        var isTax = false;
        var calculationVersion = 3;
        var eventId = Guid.NewGuid();

        // Arrange
        var amountPerChargeEvent = _amountPerChargeEventBuilder
            .WithCalculationType(AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing)
            .WithStartOfPeriod(startOfPeriod.ToTimestamp())
            .WithEndOfPeriod(endOfPeriod.ToTimestamp())
            .WithGridAreaCode(gridAreaCode)
            .WithEnergySupplier(energySupplier)
            .WithChargeCode(chargeCode)
            .WithChargeType(AmountPerChargeResultProducedV1.Types.ChargeType.Fee)
            .WithChargeOwner(chargeOwner)
            .WithQuantityUnit(AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh)
            .WithIsTax(isTax)
            .WithCurrency(AmountPerChargeResultProducedV1.Types.Currency.Dkk)
            .WithCalculationVersion(calculationVersion)
            .WithResolution(AmountPerChargeResultProducedV1.Types.Resolution.Hour)
            .Build();

        // Act
        await HandleIntegrationEventAsync(amountPerChargeEvent, eventId);

        // Assert
        var message = await AssertOutgoingMessageAsync(businessReason: BusinessReason.WholesaleFixing);

        message
            .HasProcessId(null)
            .HasEventId(eventId.ToString())
            .HasReceiverId(energySupplier)
            .HasReceiverRole(ActorRole.EnergySupplier.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasProcessType(ProcessType.ReceiveWholesaleResults)
            .HasRelationTo(null)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.CalculationVersion, calculationVersion)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.GridAreaCode, gridAreaCode)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.ChargeCode, chargeCode)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.IsTax, isTax)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.EnergySupplier, ActorNumber.Create(energySupplier))
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.ChargeOwner, ActorNumber.Create(chargeOwner))
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.Period.Start, startOfPeriod)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.Period.End, endOfPeriod)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.SettlementVersion, null)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.QuantityMeasureUnit, MeasurementUnit.Kwh)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.PriceMeasureUnit, MeasurementUnit.Kwh)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.Currency, Currency.DanishCrowns)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.ChargeType, ChargeType.Fee)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.SettlementMethod, SettlementMethod.Flex)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.MeteringPointType, MeteringPointType.Production)
            .HasMessageRecordValue<WholesaleServicesSeries>(wholesaleCalculation => wholesaleCalculation.Resolution, Resolution.Hourly);
    }

    [Theory]
    [MemberData(nameof(AllMeteringPointTypes))]
    public async Task Can_create_outgoing_message_for_all_metering_point_types(AmountPerChargeResultProducedV1.Types.MeteringPointType meteringPointType)
    {
        // Arrange
        var integrationEvent = _amountPerChargeEventBuilder
            .WithMeteringPointType(meteringPointType)
            .Build();

        // Act
        await HandleIntegrationEventAsync(integrationEvent);
        var assertOutgoingMessage = await AssertOutgoingMessageAsync();

        // Assert
        var expectedMeteringPointType = MeteringPointTypeMapper.Map(meteringPointType);

        assertOutgoingMessage
            .HasMessageRecordValue<WholesaleServicesSeries>((series) => series.MeteringPointType, expectedMeteringPointType);
    }

    private async Task HandleIntegrationEventAsync(AmountPerChargeResultProducedV1 @event, Guid? eventId = null)
    {
        var integrationEvent = new IntegrationEvent(
            eventId ?? Guid.NewGuid(),
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

    private async Task AssertOutgoingMessageIsNull(
        ActorRole? receiverRole = null,
        BusinessReason? businessReason = null)
    {
        await AssertOutgoingMessage.OutgoingMessageIsNullAsync(
            messageType: DocumentType.NotifyWholesaleServices.Name,
            businessReason: businessReason?.Name ?? BusinessReason.WholesaleFixing.Name,
            receiverRole ?? ActorRole.EnergySupplier,
            _databaseConnectionFactory);
    }

    private ServiceProvider BuildServiceProviderWithCorruptedOutgoingMessagesClient()
    {
        var serviceCollection = GetServiceCollectionClone();
        serviceCollection.RemoveAll<IOutgoingMessagesClient>();
        serviceCollection.AddScoped<IOutgoingMessagesClient, OutgoingMessageExceptionSimulator>();

        var dependenciesWithoutFileStorage = serviceCollection.BuildServiceProvider();
        return dependenciesWithoutFileStorage;
    }
}
