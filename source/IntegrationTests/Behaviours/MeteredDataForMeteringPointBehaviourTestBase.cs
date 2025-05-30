﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Globalization;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit.Abstractions;
using static Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model.ForwardMeteredDataInputV1;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test methods")]
public abstract class MeteredDataForMeteringPointBehaviourTestBase : BehavioursTestBase
{
    protected MeteredDataForMeteringPointBehaviourTestBase(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    protected BundlingOptions BundlingOptions => GetService<IOptions<BundlingOptions>>().Value;

    protected async Task<ResponseMessage> GivenReceivedMeteredDataForMeteringPoint(
        DocumentFormat documentFormat,
        ActorNumber senderActorNumber,
        MessageId messageId,
        IReadOnlyCollection<(string TransactionId, Instant PeriodStart, Instant PeriodEnd, Resolution Resolution)>
            series,
        bool assertRequestWasSuccessful = true)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = MeteredDataForMeteringPointBuilder.CreateIncomingMessage(
            messageId: messageId.Value,
            format: documentFormat,
            senderActorNumber: senderActorNumber,
            series: series);

        var response = await
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.NotifyValidatedMeasureData,
                documentFormat,
                CancellationToken.None);

        if (!assertRequestWasSuccessful)
        {
            return response;
        }

        using var scope = new AssertionScope();
        incomingMessageStream.Stream.Position = 0;
        using var reader = new StreamReader(incomingMessageStream.Stream);
        response.IsErrorResponse
            .Should()
            .BeFalse(
                "the response should not have an error. Actual response: {0}",
                response.MessageBody + "\n\n\n" + await reader.ReadToEndAsync());

        if (documentFormat != DocumentFormat.Ebix)
        {
            // The response should be empty for CIM XML and CIM JSON
            response.MessageBody.Should().BeEmpty();
        }

        return response;
    }

    protected async Task ThenNotifyValidatedMeasureDataDocumentIsCorrect(
        Stream? peekResultDocumentStream,
        DocumentFormat documentFormat,
        NotifyValidatedMeasureDataDocumentAssertionInput assertionInput)
    {
        peekResultDocumentStream.Should().NotBeNull();
        peekResultDocumentStream!.Position = 0;

        using var assertionScope = new AssertionScope();

        await NotifyValidatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(
            documentFormat,
            peekResultDocumentStream,
            assertionInput);
    }

    protected StartOrchestrationInstanceV1 ThenRequestStartForwardMeteredDataCommandV1ServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        DocumentFormat documentFormat,
        ForwardMeteredDataInputV1AssertionInput assertionInput)
    {
        var assertionResult = ThenRequestStartForwardMeteredDataCommandV1ServiceBusMessagesAreCorrect(
            senderSpy,
            [assertionInput],
            documentFormat);

        return assertionResult.Single();
    }

    protected IList<StartOrchestrationInstanceV1> ThenRequestStartForwardMeteredDataCommandV1ServiceBusMessagesAreCorrect(
        ServiceBusSenderSpy senderSpy,
        IList<ForwardMeteredDataInputV1AssertionInput> assertionInputs,
        DocumentFormat documentFormat)
    {
        var messages = AssertProcessManagerServiceBusMessages(
            senderSpy: senderSpy,
            expectedCount: assertionInputs.Count,
            parser: data => StartOrchestrationInstanceV1.Parser.ParseJson(data));

        using var assertionScope = new AssertionScope();

        var assertionMethods = assertionInputs
            .Select(x => GetAssertServiceBusMessage(x, documentFormat));

        messages
            .Select(x => x.ParseInput<ForwardMeteredDataInputV1>())
            .Should()
            .SatisfyRespectively(assertionMethods);

        return messages;
    }

    protected async Task GivenForwardMeteredDataRequestAcceptedIsReceived(ServiceBusMessage acceptedMessage)
    {
        await GivenProcessManagerResponseIsReceived(acceptedMessage);
    }

    protected async Task GivenForwardMeteredDataRequestRejectedIsReceived(ServiceBusMessage acceptedMessage)
    {
        await GivenProcessManagerResponseIsReceived(acceptedMessage);
    }

    protected NotifyOrchestrationInstanceV1 AssertCorrectProcessManagerNotification(
        ServiceBusMessage serviceBusMessage,
        NotifyOrchestrationInstanceEventV1AssertionInput assertionInput)
    {
        var message = NotifyOrchestrationInstanceV1.Parser.ParseJson(serviceBusMessage.Body.ToString());

        message.OrchestrationInstanceId.Should().BeEquivalentTo(assertionInput.InstanceId.ToString());
        message.EventName.Should().BeEquivalentTo(assertionInput.EventName);

        return message;
    }

    private static Action<ForwardMeteredDataInputV1> GetAssertServiceBusMessage(
        ForwardMeteredDataInputV1AssertionInput input,
        DocumentFormat documentFormat)
    {
        return (message) =>
        {
            message.ActorNumber.Should().BeEquivalentTo(input.ActorNumber);
            message.ActorRole.Should().BeEquivalentTo(input.ActorRole);
            message.TransactionId.Should().BeEquivalentTo(input.TransactionId.Value);
            message.MeteringPointId.Should().BeEquivalentTo(input.MeteringPointId);
            message.MeteringPointType.Should().BeEquivalentTo(input.MeteringPointType);
            message.ProductNumber.Should().BeEquivalentTo(input.ProductNumber);
            message.MeasureUnit.Should().BeEquivalentTo(input.MeasureUnit);
            message.RegistrationDateTime.Should().BeEquivalentTo(input.RegistrationDateTime.ToString());
            message.Resolution.Should().BeEquivalentTo(input.Resolution?.Name);

            if (documentFormat != DocumentFormat.Ebix)
            {
                message.StartDateTime.Should()
                    .BeEquivalentTo(input.StartDateTime.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture));
                message.EndDateTime.Should()
                    .BeEquivalentTo(input.EndDateTime?.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture));
            }
            else
            {
                message.StartDateTime.Should().BeEquivalentTo(input.StartDateTime.ToString());
                message.EndDateTime.Should().BeEquivalentTo(input.EndDateTime?.ToString());
            }

            message.GridAccessProviderNumber.Should().BeEquivalentTo(input.GridAccessProviderNumber);

            message.MeteredDataList.Should()
                .BeEquivalentTo(
                    input.EnergyObservations.Select(eo => new MeteredData(
                        eo.Position,
                        eo.EnergyQuantity,
                        eo.QuantityQuality)));
        };
    }

    private async Task GivenProcessManagerResponseIsReceived(ServiceBusMessage message)
    {
        var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            messageId: Guid.NewGuid().ToString(),
            subject: message.Subject,
            body: message.Body,
            properties: message.ApplicationProperties);
        var enqueueHandler = GetService<EnqueueHandler_Brs_021_ForwardMeteredData_V1>();
        await enqueueHandler.EnqueueAsync(serviceBusReceivedMessage, CancellationToken.None);
    }
}
