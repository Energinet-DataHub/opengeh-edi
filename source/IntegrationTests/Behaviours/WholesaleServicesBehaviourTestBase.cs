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
using Google.Protobuf;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

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
            return gridAreaCimXmlElement!.Value;
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

    protected Task GivenWholesaleServicesRequestResponseIsReceived<TType>(Guid processId, TType wholesaleServicesRequestResponseMessage)
        where TType : IMessage
    {
        return HavingReceivedInboxEventAsync(
            eventType: typeof(TType).Name,
            eventPayload: wholesaleServicesRequestResponseMessage,
            processId: processId);
    }

    protected Task<(WholesaleServicesRequest WholesaleServicesRequest, Guid ProcessId)> ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            ServiceBusSenderSpy senderSpy,
            IReadOnlyCollection<string> gridAreas,
            string requestedForActorNumber,
            string requestedForActorRole,
            string? energySupplierId,
            string? chargeOwnerId,
            string? resolution,
            string businessReason,
            IReadOnlyCollection<(string ChargeType, string ChargeCode)>? chargeTypes,
            Period period,
            string? settlementVersion)
    {
        var (message, processId) = AssertServiceBusMessage(
            senderSpy,
            (data) => WholesaleServicesRequest.Parser.ParseFrom(data));

        using var assertionScope = new AssertionScope();

        message.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        message.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        message.RequestedForActorRole.Should().Be(requestedForActorRole);

        if (energySupplierId == null)
            message.HasEnergySupplierId.Should().BeFalse();
        else
            message.EnergySupplierId.Should().Be(energySupplierId);

        if (chargeOwnerId == null)
            message.HasChargeOwnerId.Should().BeFalse();
        else
            message.ChargeOwnerId.Should().Be(chargeOwnerId);

        if (resolution == null)
            message.HasResolution.Should().BeFalse();
        else
            message.Resolution.Should().Be(resolution);

        message.BusinessReason.Should().Be(businessReason);

        if (chargeTypes == null)
        {
            message.ChargeTypes.Should().BeEmpty();
        }
        else
        {
            message.ChargeTypes.Should().BeEquivalentTo(chargeTypes.Select(ct => new Energinet.DataHub.Edi.Requests.ChargeType
            {
                ChargeType_ = ct.ChargeType,
                ChargeCode = ct.ChargeCode,
            }));
        }

        message.PeriodStart.Should().Be(period.Start.ToString());
        message.PeriodEnd.Should().Be(period.End.ToString());

        if (settlementVersion == null)
            message.HasSettlementVersion.Should().BeFalse();
        else
            message.SettlementVersion.Should().Be(settlementVersion);

        return Task.FromResult((wholesaleServicesRequestMessage: message, processId));
    }

    protected async Task ThenRejectRequestWholesaleSettlementDocumentIsCorrect(Stream? peekResultDocumentStream, DocumentFormat documentFormat, RejectRequestWholesaleSettlementDocumentAssertionInput assertionInput)
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
        IReadOnlyCollection<(string? GridArea, string TransactionId)> series,
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
            incomingMessageClient.RegisterAndSendAsync(
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
}
