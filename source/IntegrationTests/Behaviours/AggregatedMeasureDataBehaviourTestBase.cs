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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.TestData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit.Abstractions;

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
        IReadOnlyCollection<(string? GridArea, TransactionId TransactionId)> series,
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
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
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

    protected async Task GivenAggregatedMeasureDataRequestAcceptedIsReceived(ServiceBusMessage acceptedMessage)
    {
        await GivenProcessManagerResponseIsReceived(acceptedMessage);
    }

    protected async Task GivenAggregatedMeasureDataRequestRejectedIsReceived(ServiceBusMessage rejectedMessage)
    {
        await GivenProcessManagerResponseIsReceived(rejectedMessage);
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
            return gridAreaCimXmlElement.Value;
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

    protected StartOrchestrationInstanceV1 ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        RequestCalculatedEnergyTimeSeriesInputV1AssertionInput assertionInput)
    {
        var assertionResult = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessagesAreCorrect(
            senderSpy,
            new List<RequestCalculatedEnergyTimeSeriesInputV1AssertionInput> { assertionInput });

        return assertionResult.Single();
    }

    protected IList<StartOrchestrationInstanceV1> ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessagesAreCorrect(
        ServiceBusSenderSpy senderSpy,
        List<RequestCalculatedEnergyTimeSeriesInputV1AssertionInput>
            assertionInputs)
    {
        var messages = AssertProcessManagerServiceBusMessages(
            senderSpy: senderSpy,
            expectedCount: assertionInputs.Count,
            parser: data => StartOrchestrationInstanceV1.Parser.ParseJson(data));

        using var assertionScope = new AssertionScope();

        var assertionMethods = assertionInputs
            .Select(GetAssertServiceBusMessage);

        messages
            .Select(x => x.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>())
            .Should()
            .SatisfyRespectively(assertionMethods);

        return messages;
    }

    protected EnergyResultPerGridAreaDescription GivenDatabricksResultDataForEnergyResultPerGridArea()
    {
        return new EnergyResultPerGridAreaDescription();
    }

    protected EnergyResultPerBrpGridAreaDescription GivenDatabricksResultDataForEnergyResultPerBalanceResponsible()
    {
        return new EnergyResultPerBrpGridAreaDescription();
    }

    protected EnergyResultPerEnergySupplierBrpGridAreaDescription GivenDatabricksResultDataForEnergyResultPerEnergySupplier()
    {
        return new EnergyResultPerEnergySupplierBrpGridAreaDescription();
    }

    /// <summary>
    /// Assert that one of the messages is correct and don't care about the rest. We have no way of knowing which
    /// message is the correct one, so we will assert all of them and count the number of failed/successful assertions.
    /// </summary>
    protected async Task ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
        List<PeekResultDto> peekResults,
        DocumentFormat documentFormat,
        NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        var failedAssertions = new List<Exception>();
        var successfulAssertions = 0;
        foreach (var peekResultDto in peekResults)
        {
            try
            {
                using (new AssertionScope())
                {
                    await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                        peekResultDto.Bundle,
                        documentFormat,
                        assertionInput);
                }

                successfulAssertions++;
            }
            catch (Exception e)
            {
                failedAssertions.Add(e);
            }
        }

        failedAssertions.Should().HaveCount(peekResults.Count - 1);
        successfulAssertions.Should().Be(1);
    }

    private Action<RequestCalculatedEnergyTimeSeriesInputV1> GetAssertServiceBusMessage(
        RequestCalculatedEnergyTimeSeriesInputV1AssertionInput input)
    {
        return (message) =>
        {
            message.TransactionId.Should().BeEquivalentTo(input.TransactionId.Value);
            message.RequestedForActorNumber.Should().Be(input.RequestedForActorNumber);
            message.RequestedForActorRole.Should().Be(input.RequestedForActorRole);
            message.BusinessReason.Should().Be(input.BusinessReason.Name);
            message.PeriodStart.Should().Be(input.PeriodStart.ToString());
            message.PeriodEnd.Should().Be(input.PeriodEnd.ToString());
            message.EnergySupplierNumber.Should().Be(input.EnergySupplierNumber);
            message.BalanceResponsibleNumber.Should().Be(input.BalanceResponsibleNumber);
            message.GridAreas.Should().BeEquivalentTo(input.GridAreas);
            message.MeteringPointType.Should().Be(input.MeteringPointType?.Name);
            message.SettlementMethod.Should().Be(input.SettlementMethod?.Name);
            message.SettlementVersion.Should().Be(input.SettlementVersion);
        };
    }

    private async Task GivenProcessManagerResponseIsReceived(ServiceBusMessage message)
    {
        var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            messageId: Guid.NewGuid().ToString(),
            subject: message.Subject,
            body: message.Body,
            properties: message.ApplicationProperties);
        var enqueueHandler = GetService<EnqueueHandler_Brs_026_V1>();
        await enqueueHandler.EnqueueAsync(serviceBusReceivedMessage, CancellationToken.None);
    }
}
