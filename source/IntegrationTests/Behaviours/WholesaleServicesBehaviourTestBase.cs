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
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.TestData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

[SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public abstract class WholesaleServicesBehaviourTestBase : BehavioursTestBase
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    protected WholesaleServicesBehaviourTestBase(IntegrationTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
        _fixture = fixture;
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
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

    protected async Task<string> GetChargeCodeFromNotifyWholesaleServicesDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var ebixAsserter = NotifyWholesaleServicesDocumentAsserter.CreateEbixAsserter(documentStream);
            var chargeCodeElement = ebixAsserter.GetElement("PayloadEnergyTimeSeries[1]/PartyChargeTypeID");

            chargeCodeElement.Should().NotBeNull("because charge code should be present in the ebIX document");
            chargeCodeElement!.Value.Should().NotBeNull("because charge code value should not be null in the ebIX document");
            return chargeCodeElement.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlAsserter = NotifyWholesaleServicesDocumentAsserter.CreateCimXmlAsserter(documentStream);

            var chargeCodeElement = cimXmlAsserter.GetElement("Series[1]/chargeType.mRID");

            chargeCodeElement.Should().NotBeNull("because charge code should be present in the CIM XML document");
            chargeCodeElement!.Value.Should().NotBeNull("because charge code value should not be null in the CIM XML document");
            return chargeCodeElement.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var chargeCodeElement = cimJsonDocument.RootElement
                .GetProperty("NotifyWholesaleServices_MarketDocument")
                .GetProperty("Series")
                .EnumerateArray()
                .ToList()
                .Single()
                .GetProperty("chargeType.mRID");

            chargeCodeElement.Should().NotBeNull("because charge code should be present in the CIM JSON document");
            return chargeCodeElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    protected async Task<string> GetResolutionFromNotifyWholesaleServicesDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var ebixAsserter = NotifyWholesaleServicesDocumentAsserter.CreateEbixAsserter(documentStream);
            var resolutionElement = ebixAsserter.GetElement("PayloadEnergyTimeSeries[1]/ObservationTimeSeriesPeriod/ResolutionDuration");

            resolutionElement.Should().NotBeNull("because resolution should be present in the ebIX document");
            resolutionElement!.Value.Should().NotBeNull("because resolution value should not be null in the ebIX document");
            return resolutionElement.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlAsserter = NotifyWholesaleServicesDocumentAsserter.CreateCimXmlAsserter(documentStream);

            var resolutionElement = cimXmlAsserter.GetElement("Series[1]/Period/resolution");

            resolutionElement.Should().NotBeNull("because resolution should be present in the CIM XML document");
            resolutionElement!.Value.Should().NotBeNull("because resolution value should not be null in the CIM XML document");
            return resolutionElement.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var resolutionElement = cimJsonDocument.RootElement
                .GetProperty("NotifyWholesaleServices_MarketDocument")
                .GetProperty("Series")
                .EnumerateArray()
                .ToList()
                .Single()
                .GetProperty("Period")
                .GetProperty("resolution");

            resolutionElement.Should().NotBeNull("because resolution should be present in the CIM JSON document");
            return resolutionElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    protected async Task GivenWholesaleServicesRequestRejectedIsReceived(ServiceBusMessage rejectedMessage)
    {
        await GivenProcessManagerResponseIsReceived(rejectedMessage);
    }

    protected async Task GivenWholesaleServicesRequestAcceptedIsReceived(ServiceBusMessage message)
    {
        await GivenProcessManagerResponseIsReceived(message);
    }

    protected StartOrchestrationInstanceV1 ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
        ServiceBusSenderSpy senderSpy,
        RequestCalculatedWholesaleServicesInputV1AssertionInput assertionInput)
    {
        var assertionResult = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessagesAreCorrect(
            senderSpy,
            new List<RequestCalculatedWholesaleServicesInputV1AssertionInput> { assertionInput });

        return assertionResult.Single();
    }

    protected IList<StartOrchestrationInstanceV1> ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessagesAreCorrect(
        ServiceBusSenderSpy senderSpy,
        IList<RequestCalculatedWholesaleServicesInputV1AssertionInput> assertionInputs)
    {
        var messages = AssertProcessManagerServiceBusMessages(
            senderSpy: senderSpy,
            expectedCount: assertionInputs.Count,
            parser: data => StartOrchestrationInstanceV1.Parser.ParseJson(data));

        using var assertionScope = new AssertionScope();

        var assertionMethods = assertionInputs
            .Select(GetAssertServiceBusMessage);

        messages
            .Select(x => x.ParseInput<RequestCalculatedWholesaleServicesInputV1>())
            .Should()
            .SatisfyRespectively(assertionMethods);

        return messages;
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

    protected WholesaleResultForAmountPerChargeDescription GivenDatabricksResultDataForWholesaleResultAmountPerCharge()
    {
        return new WholesaleResultForAmountPerChargeDescription();
    }

    protected WholesaleResultForAmountPerChargeInTwoGridAreasDescription GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreas()
    {
        return new WholesaleResultForAmountPerChargeInTwoGridAreasDescription();
    }

    protected WholesaleResultForMonthlyAmountPerChargeDescription GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge()
    {
        return new WholesaleResultForMonthlyAmountPerChargeDescription();
    }

    protected WholesaleResultForTotalAmountDescription GivenDatabricksResultDataForWholesaleResultTotalAmount()
    {
        return new WholesaleResultForTotalAmountDescription();
    }

    private static Action<RequestCalculatedWholesaleServicesInputV1> GetAssertServiceBusMessage(
        RequestCalculatedWholesaleServicesInputV1AssertionInput input)
    {
        return (message) =>
        {
            message.TransactionId.Should().BeEquivalentTo(input.TransactionId.Value);
            message.GridAreas.Should().BeEquivalentTo(input.GridAreas);
            message.RequestedForActorNumber.Should().Be(input.RequestedForActorNumber);
            message.RequestedForActorRole.Should().Be(input.RequestedForActorRole);
            message.EnergySupplierNumber.Should().Be(input.EnergySupplierNumber);
            message.ChargeOwnerNumber.Should().Be(input.ChargeOwnerNumber);
            message.Resolution.Should().Be(input.Resolution?.Name);
            message.BusinessReason.Should().Be(input.BusinessReason.Name);
            message.PeriodStart.Should().Be(input.PeriodStart.ToString());
            message.PeriodEnd.Should().Be(input.PeriodEnd.ToString());
            message.SettlementVersion.Should().Be(input.SettlementVersion);

            if (input.ChargeTypes == null)
            {
                message.ChargeTypes.Should().BeEmpty();
            }
            else
            {
                message.ChargeTypes.Should()
                    .BeEquivalentTo(
                        input.ChargeTypes.Select(
                            ct => new RequestCalculatedWholesaleServicesInputV1.ChargeTypeInput(
                                ct.ChargeType,
                                ct.ChargeCode)));
            }
        };
    }

    private async Task GivenProcessManagerResponseIsReceived(ServiceBusMessage message)
    {
        var serviceBusReceivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            messageId: Guid.NewGuid().ToString(),
            subject: message.Subject,
            body: message.Body,
            properties: message.ApplicationProperties);
        var enqueueHandler = GetService<EnqueueHandler_Brs_028_V1>();
        await enqueueHandler.EnqueueAsync(serviceBusReceivedMessage, CancellationToken.None);
    }
}
