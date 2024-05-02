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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public abstract class AggregatedMeasureDataBehaviourTestBase : BehavioursTestBase
{
    protected AggregatedMeasureDataBehaviourTestBase(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    protected async Task<ResponseMessage> GivenReceivedAggregatedMeasureDataRequest(
        DocumentFormat documentFormat,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        (int Year, int Month, int Day) periodStart,
        (int Year, int Month, int Day) periodEnd,
        ActorNumber? energySupplier,
        ActorNumber? balanceResponsibleParty,
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series,
        bool assertRequestWasSuccessful = true)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = RequestAggregatedMeasureDataRequestBuilder.CreateIncomingMessage(
            format: documentFormat,
            senderActorNumber: senderActorNumber,
            senderActorRole: senderActorRole,
            meteringPointType: meteringPointType,
            settlementMethod: settlementMethod,
            periodStart: CreateDateInstant(periodStart.Year, periodStart.Month, periodStart.Day),
            periodEnd: CreateDateInstant(periodEnd.Year, periodEnd.Month, periodEnd.Day),
            energySupplier: energySupplier,
            balanceResponsibleParty: balanceResponsibleParty,
            series: series,
            ensureValidRequest: assertRequestWasSuccessful);

        var response = await
            incomingMessageClient.RegisterAndSendAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.RequestAggregatedMeasureData,
                documentFormat,
                CancellationToken.None);

        if (assertRequestWasSuccessful)
        {
            using var scope = new AssertionScope();
            response.IsErrorResponse.Should().BeFalse("because the response should not have an error. Actual response: {0}", response.MessageBody);
            response.MessageBody.Should().BeEmpty();
        }

        return response;
    }

    protected Task GivenAggregatedMeasureDataRequestAcceptedIsReceived(Guid processId, AggregatedTimeSeriesRequestAccepted acceptedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(AggregatedTimeSeriesRequestAccepted),
            eventPayload: acceptedMessage,
            processId: processId);
    }

    protected async Task<string> GetGridAreaFromNotifyAggregatedMeasureDataDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var ebixAsserter = NotifyAggregatedMeasureDataDocumentAsserter.CreateEbixAsserter(documentStream);
            var gridAreaElement = ebixAsserter.GetElement("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification");

            gridAreaElement.Should().NotBeNull("because grid area should be present in the ebIX document");
            gridAreaElement!.Value.Should().NotBeNull("because grid area value should not be null in the ebIX document");
            return gridAreaElement.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlAsserter = NotifyAggregatedMeasureDataDocumentAsserter.CreateCimXmlAsserter(documentStream);

            var gridAreaCimXmlElement = cimXmlAsserter.GetElement("Series[1]/meteringGridArea_Domain.mRID");

            gridAreaCimXmlElement.Should().NotBeNull("because grid area should be present in the CIM XML document");
            gridAreaCimXmlElement!.Value.Should().NotBeNull("because grid area value should not be null in the CIM XML document");
            return gridAreaCimXmlElement!.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var gridAreaCimJsonElement = cimJsonDocument.RootElement
                .GetProperty("NotifyAggregatedMeasureData_MarketDocument")
                .GetProperty("Series").EnumerateArray().ToList()
                .Single()
                .GetProperty("meteringGridArea_Domain.mRID")
                .GetProperty("value");

            gridAreaCimJsonElement.Should().NotBeNull("because grid area should be present in the CIM JSON document");
            return gridAreaCimJsonElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    protected async Task ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        RejectRequestAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await RejectRequestAggregatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected async Task ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await NotifyAggregatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected Task<(AggregatedTimeSeriesRequest AggregatedTimeSeriesRequest, Guid ProcessId)> ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            ServiceBusSenderSpy senderSpy,
            IReadOnlyCollection<string> gridAreas,
            string requestedForActorNumber,
            string requestedForActorRole,
            string? energySupplier,
            string? balanceResponsibleParty,
            BusinessReason businessReason,
            Period period,
            SettlementVersion? settlementVersion,
            SettlementMethod? settlementMethod,
            MeteringPointType? meteringPointType)
    {
        var (message, processId) = AssertServiceBusMessage(
            senderSpy,
            data => AggregatedTimeSeriesRequest.Parser.ParseFrom(data));

        using var assertionScope = new AssertionScope();

        message.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        message.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        message.RequestedForActorRole.Should().Be(requestedForActorRole);

        if (energySupplier == null)
            message.HasEnergySupplierId.Should().BeFalse();
        else
            message.EnergySupplierId.Should().Be(energySupplier);

        if (balanceResponsibleParty == null)
            message.HasBalanceResponsibleId.Should().BeFalse();
        else
            message.BalanceResponsibleId.Should().Be(balanceResponsibleParty);

        message.BusinessReason.Should().Be(businessReason.Name);

        message.Period.Start.Should().Be(period.Start.ToString());
        message.Period.End.Should().Be(period.End.ToString());

        if (settlementVersion == null)
            message.HasSettlementVersion.Should().BeFalse();
        else
            message.SettlementVersion.Should().Be(settlementVersion.Name);

        if (settlementMethod == null)
            message.HasSettlementMethod.Should().BeFalse();
        else
            message.SettlementMethod.Should().Be(settlementMethod.Name);

        if (meteringPointType == null)
            message.MeteringPointType.Should().BeEmpty(); // Contract is incorrect and doesn't have MeteringPointType as optional
        else
            message.MeteringPointType.Should().Be(meteringPointType.Name);

        return Task.FromResult((message, processId));
    }

    protected async Task WhenAggregatedMeasureDataProcessIsInitialized(ServiceBusMessage serviceBusMessage)
    {
        await InitializeProcess(serviceBusMessage, nameof(InitializeAggregatedMeasureDataProcessDto));
    }

    protected async Task GivenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(
        ServiceBusMessage serviceBusMessage)
    {
        // We have to manually process the service bus message, as there isn't a real service bus
        serviceBusMessage.Subject.Should().Be(nameof(InitializeAggregatedMeasureDataProcessDto));
        serviceBusMessage.Body.Should().NotBeNull();

        await GetService<IProcessClient>().InitializeAsync(serviceBusMessage.Subject, serviceBusMessage.Body.ToArray());
        await ProcessInternalCommandsAsync();
    }

    protected async Task WhenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(
        ServiceBusMessage serviceBusMessage)
    {
        await GivenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(serviceBusMessage);
    }

    protected async Task GivenWholesaleAcceptedResponseToAggregatedMeasureDataRequestAsync(
        ServiceBusMessage serviceBusMessage)
    {
        serviceBusMessage.Subject.Should().Be(nameof(AggregatedTimeSeriesRequest));
        serviceBusMessage.Body.Should().NotBeNull();

        var aggregatedTimeSeriesRequest =
            AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);

        var aggregatedTimeSeriesRequestAccepted =
            AggregatedTimeSeriesResponseEventBuilder.GenerateAcceptedFrom(aggregatedTimeSeriesRequest, GetNow());

        await HavingReceivedInboxEventAsync(
            nameof(AggregatedTimeSeriesRequestAccepted),
            aggregatedTimeSeriesRequestAccepted,
            Guid.Parse(serviceBusMessage.MessageId));
    }
}
