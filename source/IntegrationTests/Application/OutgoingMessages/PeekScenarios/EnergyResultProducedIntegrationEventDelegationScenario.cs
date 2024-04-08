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
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.PeekScenarios;

public class EnergyResultProducedIntegrationEventDelegationScenario : IIntegrationEventScenario
{
    private readonly DocumentFormat _documentFormat;
    private readonly ActorNumberAndRoleDto _delegatedBy = CreateActorNumberAndRole(ActorNumber.Create("1234567891234"));
    private readonly ActorNumberAndRoleDto _delegatedTo = CreateActorNumberAndRole(ActorNumber.Create("1234567891235"), actorRole: ActorRole.Delegated);
    private readonly string _gridAreaCode = "805";
    private EnergyResultProducedV2? _energyProducedEvent;

    public EnergyResultProducedIntegrationEventDelegationScenario(DocumentFormat documentFormat)
    {
        _documentFormat = documentFormat;
    }

    public async Task<IntegrationEvent> BuildAsync(ServiceProvider serviceProvider)
    {
        _energyProducedEvent = new EnergyResultProducedV2EventBuilder()
            .AggregatedBy(_gridAreaCode, balanceResponsibleNumber: _delegatedBy.ActorNumber.Value)
            .WithPointsForPeriod()
            .Build();

        await AddDelegationAsync(serviceProvider, _delegatedBy, _delegatedTo, _gridAreaCode);

        return new IntegrationEvent(Guid.NewGuid(), EnergyResultProducedV2.EventName, 1, _energyProducedEvent);
    }

    public async Task AssertAsync(ServiceProvider serviceProvider)
    {
        var document = await PeekMessageAsync(serviceProvider, _delegatedTo);

        await GetDocumentAsserter(document.Bundle!)
            .HasMessageId()
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasReceiverId(_delegatedTo.ActorNumber.Value)
            .HasReceiverRole(ActorRole.BalanceResponsibleParty.Code)
            .HasTransactionId()
            .HasGridAreaCode(_energyProducedEvent!.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode)
            .HasBalanceResponsibleNumber(_energyProducedEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId)
            .HasProductCode(ProductType.EnergyActive.Code)
            .HasPeriod(
                new Period(_energyProducedEvent.PeriodStartUtc.ToInstant(), _energyProducedEvent.PeriodEndUtc.ToInstant()))
            .HasPoint(1, 1.000000001m)
            .HasSettlementMethod(Map(_energyProducedEvent!.TimeSeriesType)!)
            .HasCalculationResultVersion(_energyProducedEvent.CalculationResultVersion)
            .DocumentIsValidAsync();
    }

    private static ActorNumberAndRoleDto CreateActorNumberAndRole(ActorNumber actorNumber, ActorRole? actorRole = null)
    {
        return new ActorNumberAndRoleDto(actorNumber, actorRole ?? ActorRole.BalanceResponsibleParty);
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
                processType ?? ProcessType.ReceiveEnergyResults,
                gridAreaCode,
                startsAt ?? SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                stopsAt ?? SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(5)),
                delegatedBy,
                delegatedTo),
            CancellationToken.None);
    }

    private static SettlementMethod? Map(EnergyResultProducedV2.Types.TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            // There exist no corresponding SettlementMethod for these TimeSeriesTypes
            EnergyResultProducedV2.Types.TimeSeriesType.Production or
                EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa or
                EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerNeighboringGa or
                EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption => null,

            EnergyResultProducedV2.Types.TimeSeriesType.FlexConsumption => SettlementMethod.Flex,
            EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption => SettlementMethod.NonProfiled,
            EnergyResultProducedV2.Types.TimeSeriesType.Unspecified => throw new InvalidOperationException("Could not map time series type"),
            _ => throw new ArgumentOutOfRangeException(nameof(timeSeriesType), timeSeriesType, "Unknown time series type from Wholesale"),
        };
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

    private IAssertNotifyAggregatedMeasureDataDocument GetDocumentAsserter(Stream document)
    {
        switch (_documentFormat)
        {
            case var _ when DocumentFormat.Xml == _documentFormat:
                var assertXmlDocument = AssertXmlDocument.Document(document, "cim", new DocumentValidator(new[] { new CimXmlValidator(new CimXmlSchemaProvider()) }));
                return new AssertNotifyAggregatedMeasureDataXmlDocument(assertXmlDocument);
            case var _ when DocumentFormat.Ebix == _documentFormat:
                var assertEbixDocument = AssertEbixDocument.Document(document, "ns0", new DocumentValidator(new[] { new EbixValidator(new EbixSchemaProvider()) }));
                return new AssertNotifyAggregatedMeasureDataEbixDocument(assertEbixDocument);
            case var _ when DocumentFormat.Json == _documentFormat:
                return new AssertNotifyAggregatedMeasureDataJsonDocument(document);
        }

        throw new MissingMemberException();
    }
}
