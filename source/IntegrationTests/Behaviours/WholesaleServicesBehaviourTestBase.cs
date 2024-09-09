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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public abstract class WholesaleServicesBehaviourTestBase : BehavioursTestBase
{
    protected WholesaleServicesBehaviourTestBase(IntegrationTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    protected async Task<string> GetGridAreaFromNotifyWholesaleServicesDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var ebixAsserter = NotifyWholesaleServicesDocumentAsserter.CreateEbixAsserter(documentStream);
            var gridAreaElement = ebixAsserter.GetElement("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification");

            gridAreaElement.Should().NotBeNull("because grid area should be present in the ebIX document");
            gridAreaElement!.Value.Should().NotBeNull("because grid area value should not be null in the ebIX document");
            return gridAreaElement.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlAsserter = NotifyWholesaleServicesDocumentAsserter.CreateCimXmlAsserter(documentStream);

            var gridAreaCimXmlElement = cimXmlAsserter.GetElement("Series[1]/meteringGridArea_Domain.mRID");

            gridAreaCimXmlElement.Should().NotBeNull("because grid area should be present in the CIM XML document");
            gridAreaCimXmlElement!.Value.Should().NotBeNull("because grid area value should not be null in the CIM XML document");
            return gridAreaCimXmlElement.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var gridAreaCimJsonElement = cimJsonDocument.RootElement
                .GetProperty("NotifyWholesaleServices_MarketDocument")
                .GetProperty("Series").EnumerateArray().ToList()
                .Single()
                .GetProperty("meteringGridArea_Domain.mRID")
                .GetProperty("value");

            gridAreaCimJsonElement.Should().NotBeNull("because grid area should be present in the CIM JSON document");
            return gridAreaCimJsonElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    protected Task GivenWholesaleServicesRequestAcceptedIsReceived(Guid processId, WholesaleServicesRequestAccepted acceptedMessage)
    {
        return GivenWholesaleServicesRequestResponseIsReceived(processId, acceptedMessage);
    }

    protected Task GivenWholesaleServicesRequestRejectedIsReceived(Guid processId, WholesaleServicesRequestRejected rejectedMessage)
    {
        return GivenWholesaleServicesRequestResponseIsReceived(processId, rejectedMessage);
    }

    protected async Task<(WholesaleServicesRequest WholesaleServicesRequest, Guid ProcessId)> ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        WholesaleServicesMessageAssertionInput assertionInput)
    {
        var assertionResult = await ThenWholesaleServicesRequestServiceBusMessagesAreCorrect(
            senderSpy,
            new List<WholesaleServicesMessageAssertionInput> { assertionInput });

        return assertionResult.Single();
    }

    protected Task<IList<(WholesaleServicesRequest WholesaleServicesRequest, Guid ProcessId)>> ThenWholesaleServicesRequestServiceBusMessagesAreCorrect(
        ServiceBusSenderSpy senderSpy,
        IList<WholesaleServicesMessageAssertionInput> assertionInputs)
    {
        var messages = AssertServiceBusMessages(
            senderSpy: senderSpy,
            expectedCount: assertionInputs.Count,
            parser: data => WholesaleServicesRequest.Parser.ParseFrom(data));

        using var assertionScope = new AssertionScope();

        var assertionMethods = assertionInputs
            .OrderBy(i => string.Join(",", i.GridAreas))
            .Select(GetAssertServiceBusMessage);

        messages.Select(m => m.Message)
            .OrderBy(m => string.Join(",", m.GridAreaCodes))
            .Should()
            .SatisfyRespectively(assertionMethods);

        return Task.FromResult(messages);
    }

    protected async Task ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        RejectRequestWholesaleSettlementDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await RejectRequestWholesaleSettlementDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected async Task ThenNotifyWholesaleServicesDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        NotifyWholesaleServicesDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await NotifyWholesaleServicesDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected async Task WhenWholesaleServicesProcessIsInitialized(ServiceBusMessage serviceBusMessage)
    {
        await InitializeProcess(serviceBusMessage, nameof(InitializeWholesaleServicesProcessDto));
    }

    protected async Task WhenWholesaleServicesProcessesAreInitialized(IList<ServiceBusMessage> serviceBusMessages)
    {
        foreach (var serviceBusMessage in serviceBusMessages)
        {
            await InitializeProcess(serviceBusMessage, nameof(InitializeWholesaleServicesProcessDto));
        }
    }

    protected async Task<ResponseMessage> GivenReceivedWholesaleServicesRequest(
        DocumentFormat documentFormat,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        (int Year, int Month, int Day) periodStart,
        (int Year, int Month, int Day) periodEnd,
        ActorNumber? energySupplier,
        ActorNumber? chargeOwner,
        string? chargeCode,
        ChargeType? chargeType,
        bool isMonthly,
        IReadOnlyCollection<(string? GridArea, TransactionId TransactionId)> series,
        bool assertRequestWasSuccessful = true)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = RequestWholesaleServicesRequestBuilder.GetStream(
            documentFormat,
            senderActorNumber,
            senderActorRole,
            CreateDateInstant(periodStart.Year, periodStart.Month, periodStart.Day),
            CreateDateInstant(periodEnd.Year, periodEnd.Month, periodEnd.Day),
            energySupplier,
            chargeOwner,
            chargeCode,
            chargeType,
            isMonthly,
            series);

        var response = await
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.RequestWholesaleSettlement,
                documentFormat,
                CancellationToken.None);

        if (assertRequestWasSuccessful)
        {
            using var scope = new AssertionScope();
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeEmpty();
        }

        return response;
    }

    private static Action<WholesaleServicesRequest> GetAssertServiceBusMessage(
        WholesaleServicesMessageAssertionInput input)
    {
        return (message) =>
        {
            message.GridAreaCodes.Should().BeEquivalentTo(input.GridAreas);
            message.RequestedForActorNumber.Should().Be(input.RequestedForActorNumber);
            message.RequestedForActorRole.Should().Be(input.RequestedForActorRole);

            if (input.EnergySupplierId == null)
                message.HasEnergySupplierId.Should().BeFalse();
            else
                message.EnergySupplierId.Should().Be(input.EnergySupplierId);

            if (input.ChargeOwnerId == null)
                message.HasChargeOwnerId.Should().BeFalse();
            else
                message.ChargeOwnerId.Should().Be(input.ChargeOwnerId);

            if (input.Resolution == null)
                message.HasResolution.Should().BeFalse();
            else
                message.Resolution.Should().Be(input.Resolution);

            message.BusinessReason.Should().Be(input.BusinessReason);

            if (input.ChargeTypes == null)
            {
                message.ChargeTypes.Should().BeEmpty();
            }
            else
            {
                message.ChargeTypes.Should()
                    .BeEquivalentTo(
                        input.ChargeTypes.Select(
                            ct => new Edi.Requests.ChargeType
                            {
                                ChargeType_ = ct.ChargeType, ChargeCode = ct.ChargeCode ?? string.Empty,
                            }));
            }

            message.PeriodStart.Should().Be(input.Period.Start.ToString());
            message.PeriodEnd.Should().Be(input.Period.End.ToString());

            if (input.SettlementVersion == null)
                message.HasSettlementVersion.Should().BeFalse();
            else
                message.SettlementVersion.Should().Be(input.SettlementVersion);
        };
    }

    private Task GivenWholesaleServicesRequestResponseIsReceived<TType>(Guid processId, TType wholesaleServicesRequestResponseMessage)
        where TType : IMessage
    {
        return HavingReceivedInboxEventAsync(
            eventType: typeof(TType).Name,
            eventPayload: wholesaleServicesRequestResponseMessage,
            processId: processId);
    }
}
