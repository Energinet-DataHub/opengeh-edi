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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Ebix;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using OutgoingMessages.Application.Tests.MarketDocuments.Asserts;
using OutgoingMessages.Application.Tests.MarketDocuments.NotifyAggregatedMeasureData;
using OutgoingMessages.Application.Tests.MarketDocuments.NotifyWholesaleServices;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.PeekScenarios;

public class AmountPerChargeResultProducedIntegrationEventDelegationScenario : IIntegrationEventScenario
{
    private readonly DocumentFormat _documentFormat;
    private readonly ActorNumberAndRoleDto _delegatedBy = CreateActorNumberAndRole(ActorNumber.Create("1234567891234"), actorRole: ActorRole.EnergySupplier);
    private readonly ActorNumberAndRoleDto _delegatedTo = CreateActorNumberAndRole(ActorNumber.Create("1234567891235"), actorRole: ActorRole.Delegated);
    private readonly string _gridAreaCode = "805";
    private AmountPerChargeResultProducedV1? _amountPerChargeResultProducedV1;

    public AmountPerChargeResultProducedIntegrationEventDelegationScenario(DocumentFormat documentFormat)
    {
        _documentFormat = documentFormat;
    }

    public async Task<IntegrationEvent> BuildAsync(ServiceProvider serviceProvider)
    {
        _amountPerChargeResultProducedV1 = new AmountPerChargeResultProducedV1EventBuilder()
            .WithEnergySupplier(_delegatedBy.ActorNumber.Value)
            .WithGridAreaCode(_gridAreaCode)
            .Build();

        await AddDelegationAsync(serviceProvider, _delegatedBy, _delegatedTo, _gridAreaCode);

        return new IntegrationEvent(Guid.NewGuid(), AmountPerChargeResultProducedV1.EventName, 1, _amountPerChargeResultProducedV1);
    }

    public async Task AssertAsync(ServiceProvider serviceProvider)
    {
        var document = await PeekMessageAsync(serviceProvider, _delegatedTo);

        await GetDocumentAsserter(document.Bundle!)
                .HasMessageId()
                .HasBusinessReason(BusinessReason.WholesaleFixing, CodeListType.EbixDenmark)
                .HasSenderId(DataHubDetails.DataHubActorNumber, "A10")
                .HasSenderRole(ActorRole.MeteredDataAdministrator)
                .HasReceiverId(_delegatedTo.ActorNumber)
                .HasReceiverRole(_delegatedBy.ActorRole, CodeListType.Ebix) // MDR is from CodeListType.Ebix
                .HasTransactionId()
                .HasCalculationVersion(_amountPerChargeResultProducedV1!.CalculationResultVersion)
                .HasChargeCode(_amountPerChargeResultProducedV1.ChargeCode)
                .HasChargeType(ChargeType.FromName(_amountPerChargeResultProducedV1.ChargeType.ToString()))
                .HasChargeTypeOwner(ActorNumber.Create(_amountPerChargeResultProducedV1.ChargeOwnerId), "A10")
                .HasGridAreaCode(_amountPerChargeResultProducedV1.GridAreaCode, "NDK")
                .HasEnergySupplierNumber(ActorNumber.Create(_amountPerChargeResultProducedV1.EnergySupplierId), "A10")
                .HasPeriod(new BuildingBlocks.Domain.Models.Period(_amountPerChargeResultProducedV1.PeriodStartUtc.ToInstant(), _amountPerChargeResultProducedV1.PeriodEndUtc.ToInstant()))
                .HasCurrency(Currency.FromCode(_amountPerChargeResultProducedV1.Currency.ToString()))
                .HasMeasurementUnit(MeasurementUnit.FromCode(_amountPerChargeResultProducedV1.QuantityUnit.ToString()))
                .HasPriceMeasurementUnit(MeasurementUnit.Kwh)
                .HasResolution(Resolution.Daily)
                .HasPositionAndQuantity(1, 0M)
                .HasProductCode(ProductType.Tariff.Code)
                .SettlementVersionIsNotPresent()
                .DocumentIsValidAsync().ConfigureAwait(false);

        var chargeOwnerNumber = ActorNumber.Create(_amountPerChargeResultProducedV1.ChargeOwnerId);
        var chargeOwnerRole = GetChargeOwnerRole(chargeOwnerNumber);
        var documentForChargeOwner = await PeekMessageAsync(serviceProvider, new ActorNumberAndRoleDto(chargeOwnerNumber, chargeOwnerRole));
        await GetDocumentAsserter(documentForChargeOwner.Bundle!)
                .HasMessageId()
                .HasBusinessReason(BusinessReason.WholesaleFixing, CodeListType.EbixDenmark)
                .HasSenderId(DataHubDetails.DataHubActorNumber, "A10")
                .HasSenderRole(ActorRole.MeteredDataAdministrator)
                .HasReceiverId(ActorNumber.Create(_amountPerChargeResultProducedV1.ChargeOwnerId))
                .HasReceiverRole(ActorRole.GridOperator, CodeListType.Ebix) // MDR is from CodeListType.Ebix
                .HasTransactionId()
                .HasCalculationVersion(_amountPerChargeResultProducedV1.CalculationResultVersion)
                .HasChargeCode(_amountPerChargeResultProducedV1!.ChargeCode)
                .HasChargeType(ChargeType.FromName(_amountPerChargeResultProducedV1.ChargeType.ToString()))
                .HasChargeTypeOwner(ActorNumber.Create(_amountPerChargeResultProducedV1.ChargeOwnerId), "A10")
                .HasGridAreaCode(_amountPerChargeResultProducedV1.GridAreaCode, "NDK")
                .HasEnergySupplierNumber(ActorNumber.Create(_amountPerChargeResultProducedV1.EnergySupplierId), "A10")
                .HasPeriod(new BuildingBlocks.Domain.Models.Period(_amountPerChargeResultProducedV1.PeriodStartUtc.ToInstant(), _amountPerChargeResultProducedV1.PeriodEndUtc.ToInstant()))
                .HasCurrency(Currency.FromCode(_amountPerChargeResultProducedV1.Currency.ToString()))
                .HasMeasurementUnit(MeasurementUnit.FromCode(_amountPerChargeResultProducedV1.QuantityUnit.ToString()))
                .HasPriceMeasurementUnit(MeasurementUnit.Kwh)
                .HasResolution(Resolution.Daily)
                .HasPositionAndQuantity(1, 0M)
                .HasProductCode(ProductType.Tariff.Code)
                .SettlementVersionIsNotPresent()
                .DocumentIsValidAsync().ConfigureAwait(false);
    }

    private static ActorNumberAndRoleDto CreateActorNumberAndRole(ActorNumber actorNumber, ActorRole? actorRole = null)
    {
        return new ActorNumberAndRoleDto(actorNumber, actorRole ?? ActorRole.BalanceResponsibleParty);
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.DataHubActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }

    private static async Task AddDelegationAsync(
        ServiceProvider serviceProvider,
        ActorNumberAndRoleDto delegatedBy,
        ActorNumberAndRoleDto delegatedTo,
        string gridAreaCode,
        ProcessType? processType = null,
        Instant? startsAt = null,
        Instant? stopsAt = null,
        int sequenceNumber = 0)
    {
        var masterDataClient = serviceProvider.GetRequiredService<IMasterDataClient>();
        await masterDataClient.CreateProcessDelegationAsync(
            new ProcessDelegationDto(
                sequenceNumber,
                processType ?? ProcessType.ReceiveWholesaleResults,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }

    private Task<PeekResultDto> PeekMessageAsync(ServiceProvider serviceProvider, ActorNumberAndRoleDto receiver)
    {
        var outgoingMessagesClient = serviceProvider.GetRequiredService<IOutgoingMessagesClient>();
        return outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                receiver.ActorNumber,
                MessageCategory.Aggregations,
                receiver.ActorRole,
                _documentFormat),
            CancellationToken.None);
    }

    private IAssertNotifyWholesaleServicesDocument GetDocumentAsserter(Stream document)
    {
        switch (_documentFormat)
        {
            case var _ when DocumentFormat.Xml == _documentFormat:
                var assertXmlDocument = AssertXmlDocument.Document(document, "cim", new DocumentValidator(new[] { new CimXmlValidator(new CimXmlSchemaProvider()) }));
                return new AssertNotifyWholesaleServicesXmlDocument(assertXmlDocument);
            case var _ when DocumentFormat.Ebix == _documentFormat:
                var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", new DocumentValidator(new[] { new EbixValidator(new EbixSchemaProvider()) }));
                return new AssertNotifyWholesaleServicesEbixDocument(assertEbixDocument);
            case var _ when DocumentFormat.Json == _documentFormat:
                return new AssertNotifyWholesaleServicesJsonDocument(document);
        }

        throw new MissingMemberException();
    }
}
