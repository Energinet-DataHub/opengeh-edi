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
using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test methods")]
public class RequestValidatedMeasurementsBehaviourTestBase(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : BehavioursTestBase(integrationTestFixture, testOutputHelper)
{
    internal async Task GivenRequestValidatedMeasurements(
        DocumentFormat documentFormat,
        Actor senderActor,
        MessageId messageId,
        IReadOnlyCollection<(TransactionId TransactionId, Instant PeriodStart, Instant PeriodEnd, MeteringPointId MeteringPointId)>
            series)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var incomingMessageStream = RequestValidatedMeasurementsBuilder.CreateIncomingMessage(
            messageId: messageId.Value,
            format: documentFormat,
            senderActor: senderActor,
            series: series);

        var response = await
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                documentFormat,
                IncomingDocumentType.RequestMeasurements,
                documentFormat,
                CancellationToken.None);

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
    }

    internal StartOrchestrationInstanceV1 ThenRequestValidatedMeasurementsInputV1ServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        DocumentFormat documentFormat,
        RequestValidatedMeasurementsInputV1AssertionInput assertionInput)
    {
        var assertionResult = ThenRequestValidatedMeasurementsInputV1ServiceBusMessagesIsCorrect(
            senderSpy,
            [assertionInput],
            documentFormat);

        return assertionResult.Single();
    }

    internal IList<StartOrchestrationInstanceV1> ThenRequestValidatedMeasurementsInputV1ServiceBusMessagesIsCorrect(
        ServiceBusSenderSpy senderSpy,
        IList<RequestValidatedMeasurementsInputV1AssertionInput> assertionInputs,
        DocumentFormat documentFormat)
    {
        var messages = AssertProcessManagerServiceBusMessages(
            senderSpy: senderSpy,
            expectedCount: assertionInputs.Count,
            parser: data => StartOrchestrationInstanceV1.Parser.ParseJson(data));

        using var assertionScope = new AssertionScope();

        var assertionMethods = assertionInputs
            .Select(GetAssertServiceBusMessage);

        messages
            .Select(x => x.ParseInput<RequestValidatedMeasurementsInputV1>())
            .Should()
            .SatisfyRespectively(assertionMethods);

        return messages;
    }

    private static Action<RequestValidatedMeasurementsInputV1> GetAssertServiceBusMessage(
        RequestValidatedMeasurementsInputV1AssertionInput input)
    {
        return (message) =>
        {
            message.RequestedForActorNumber.Should().Be(input.RequestedForActor.ActorNumber.Value);
            message.RequestedForActorRole.Should().Be(input.RequestedForActor.ActorRole.Name);
            message.BusinessReason.Should().Be(input.BusinessReason.Name);
            message.TransactionId.Should().Be(input.TransactionId.Value);
            message.MeteringPointId.Should().Be(input.MeteringPointId.Value);
            message.PeriodStart.Should().Be(input.StartDateTime.ToString());
            message.PeriodEnd.Should().Be(input.EndDateTime?.ToString());
        };
    }
}
