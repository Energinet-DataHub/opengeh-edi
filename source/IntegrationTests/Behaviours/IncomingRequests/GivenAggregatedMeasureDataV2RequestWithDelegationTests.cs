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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

// ReSharper disable InconsistentNaming

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test class")]
[SuppressMessage(
    "StyleCop.CSharp.OrderingRules",
    "SA1201:Elements should appear in the correct order",
    Justification = "Test class")]
public class GivenAggregatedMeasureDataV2RequestWithDelegationTests
    : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenAggregatedMeasureDataV2RequestWithDelegationTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        FeatureFlagManagerStub.SetFeatureFlag(
            FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration,
            true);
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public static object[][] DocumentFormatsWithAllRoleCombinations()
        => DocumentFormatsWithRoleCombinations(false);

    public static object[][] DocumentFormatsWithRoleCombinationsForNullGridArea()
        => DocumentFormatsWithRoleCombinations(true);

    public static object[][] DocumentFormatsWithRoleCombinations(bool nullGridArea)
    {
        var roleCombinations = new List<(ActorRole DelegatedFrom, ActorRole DelegatedTo)>
        {
            // Energy supplier, metered data responsible and balance responsible can only delegate to delegated
            (ActorRole.EnergySupplier, ActorRole.Delegated), (ActorRole.BalanceResponsibleParty, ActorRole.Delegated),
        };

        // Grid operator and MDR cannot make request with null grid area
        if (!nullGridArea)
        {
            roleCombinations.Add((ActorRole.MeteredDataResponsible, ActorRole.Delegated));
            roleCombinations.Add((ActorRole.GridAccessProvider, ActorRole.Delegated));

            // Grid operator can delegate to both delegated and grid operator
            roleCombinations.Add((ActorRole.GridAccessProvider, ActorRole.GridAccessProvider));
        }

        var requestDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(
                except: new[]
                {
                    DocumentFormat.Ebix.Name, // ebIX is not supported for requests
                })
            .ToArray();

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return roleCombinations.SelectMany(
                d => requestDocumentFormats
                    .SelectMany(
                        incomingDocumentFormat => peekDocumentFormats
                            .Select(
                                peekDocumentFormat => new object[]
                                {
                                    incomingDocumentFormat, peekDocumentFormat, d.DelegatedFrom, d.DelegatedTo,
                                })))
            .ToArray();
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return ["Xml", ActorRole.MeteredDataResponsible.Name];
        yield return ["Json", ActorRole.MeteredDataResponsible.Name];
        yield return ["Xml", ActorRole.GridAccessProvider.Name];
        yield return ["Json", ActorRole.GridAccessProvider.Name];
        yield return ["Xml", ActorRole.EnergySupplier.Name];
        yield return ["Json", ActorRole.EnergySupplier.Name];
        yield return ["Xml", ActorRole.BalanceResponsibleParty.Name];
        yield return ["Json", ActorRole.BalanceResponsibleParty.Name];
    }

    [Theory]
    [MemberData(
        nameof(DocumentFormatsWithAllRoleCombinations),
        MemberType = typeof(GivenAggregatedMeasureDataV2RequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneNotifyAggregatedMeasureDataWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider
                            || delegatedFromRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: delegatedFromRole);

        var gridAreaWithDelegation = "543";
        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync(gridAreaWithDelegation, gridAreaOwner);
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaWithDelegation,
            ProcessType.RequestEnergyResults,
            GetNow());

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[] { (gridAreaWithDelegation, transactionId), });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                originalActor.ActorNumber.Value,
                originalActor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber?.Value,
                balanceResponsibleParty?.Value,
                new List<string> { testMessageData.ExampleMessageData.GridArea },
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should()
                .BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should()
                .ContainSingle("because there should only be one message for one grid area")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: delegatedToActor.ActorNumber,
                SenderId: DataHubDetails.DataHubActorNumber,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                GridAreaCode: gridAreaWithDelegation,
                OriginalTransactionIdReference: transactionId,
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                CalculationVersion: testMessageData.ExampleMessageData.Version,
                Resolution: testMessageData.ExampleMessageData.Resolution,
                Period: new Period(
                    CreateDateInstant(2022, 01, 12),
                    CreateDateInstant(2022, 01, 13)),
                Points: testMessageData.ExampleMessageData.Points));
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/:u:/r/sites/DH3ART-team/Delte%20dokumenter/General/CIM/CIM%20XSD%20-%20XML/XML%20filer/XML%2020220706%20-%20Danske%20koder%20-%20v.1.5/Reject%20request%20aggregated%20measure%20data.xml?csf=1&web=1&e=F5bcPI
    /// </summary>
    [Theory]
    [MemberData(
        nameof(DocumentFormatsWithAllRoleCombinations),
        MemberType = typeof(GivenAggregatedMeasureDataV2RequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_AndGiven_InvalidRequest_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectAggregatedMeasureDataDocumentsWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider
                            || delegatedFromRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: delegatedFromRole);

        var gridAreaWithDelegation = "543";
        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync(gridAreaWithDelegation, gridAreaOwner);
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaWithDelegation,
            ProcessType.RequestEnergyResults,
            GetNow());

        // Act
        // Setup fake request (period is more than a month)
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 3, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[] { (gridAreaWithDelegation, transactionId), });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                originalActor.ActorNumber.Value,
                originalActor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 3, 1),
                energySupplierNumber?.Value,
                balanceResponsibleParty?.Value,
                new List<string> { testMessageData.ExampleMessageData.GridArea },
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Dato må kun være for 1 måned af gangen / Can maximum be for a 1 month period";
        var expectedErrorCode = "E17";

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesRejected = AggregatedTimeSeriesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedEnergyTimeSeriesInput, expectedErrorMessage, expectedErrorCode);

        await GivenAggregatedMeasureDataRequestRejectedIsReceived(
            requestCalculatedEnergyTimeSeriesRejected);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should()
                .BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should()
                .ContainSingle("because there should only be one rejected message regardless of grid areas")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new(
                BusinessReason.BalanceFixing,
                "5790001330552",
                delegatedToActor.ActorNumber.Value,
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                ReasonCode.FullyRejected.Code,
                TransactionId.From("12356478912356478912356478912356478"),
                expectedErrorCode,
                expectedErrorMessage));
    }

    [Theory]
    [MemberData(
        nameof(DocumentFormatsWithRoleCombinationsForNullGridArea),
        MemberType = typeof(GivenAggregatedMeasureDataV2RequestWithDelegationTests))]
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyAggregatedMeasureDataDocumentsWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider
                            || delegatedFromRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: delegatedFromRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var gridAreasWithDelegation = new List<string>() { "542", "543" };

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        foreach (var gridAreaWithDelegation in gridAreasWithDelegation)
        {
            await GivenGridAreaOwnershipAsync(gridAreaWithDelegation, gridAreaOwner);

            await GivenDelegation(
                new(originalActor.ActorNumber, originalActor.ActorRole),
                new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
                gridAreaWithDelegation,
                ProcessType.RequestEnergyResults,
                GetNow());
        }

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[] { (null, transactionId), });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                originalActor.ActorNumber.Value,
                originalActor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber?.Value,
                balanceResponsibleParty?.Value,
                gridAreasWithDelegation,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            originalActorPeekResults.Should()
                .BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            delegatedActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in delegatedActorPeekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea =
                await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyAggregatedMeasureDataDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.BalanceFixing,
                        null),
                    ReceiverId: delegatedToActor.ActorNumber,
                    SenderId: DataHubDetails.DataHubActorNumber,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                    MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                    GridAreaCode: peekResultGridArea,
                    OriginalTransactionIdReference: transactionId,
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                    CalculationVersion: testMessageData.ExampleMessageData.Version,
                    Resolution: testMessageData.ExampleMessageData.Resolution,
                    Period: new Period(
                        CreateDateInstant(2022, 01, 12),
                        CreateDateInstant(2022, 01, 13)),
                    Points: testMessageData.ExampleMessageData.Points));
        }

        resultGridAreas.Should().BeEquivalentTo("542", "543");
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/:u:/r/sites/DH3ART-team/Delte%20dokumenter/General/CIM/CIM%20XSD%20-%20XML/XML%20filer/XML%2020220706%20-%20Danske%20koder%20-%20v.1.5/Reject%20request%20aggregated%20measure%20data.xml?csf=1&web=1&e=F5bcPI
    /// </summary>
    [Theory]
    [MemberData(
        nameof(DocumentFormatsWithRoleCombinationsForNullGridArea),
        MemberType = typeof(GivenAggregatedMeasureDataV2RequestWithDelegationTests))]
    public async Task AndGiven_DelegationInTwoGridAreas_AndGiven_InvalidRequest_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectAggregatedMeasureDataDocumentsWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider
                            || delegatedFromRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: delegatedFromRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        var gridAreasWithDelegation = new List<string>() { "542", "543" };
        foreach (var gridAreaWithDelegation in gridAreasWithDelegation)
        {
            await GivenGridAreaOwnershipAsync(gridAreaWithDelegation, gridAreaOwner);

            await GivenDelegation(
                new(originalActor.ActorNumber, originalActor.ActorRole),
                new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
                gridAreaWithDelegation,
                ProcessType.RequestEnergyResults,
                GetNow());
        }

        // Act
        // Setup fake request (period is more than a month)
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 3, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[] { (null, transactionId), });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                originalActor.ActorNumber.Value,
                originalActor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 3, 1),
                energySupplierNumber?.Value,
                balanceResponsibleParty?.Value,
                gridAreasWithDelegation,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Dato må kun være for 1 måned af gangen / Can maximum be for a 1 month period";
        var expectedErrorCode = "E17";

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesRejected = AggregatedTimeSeriesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedEnergyTimeSeriesInput, expectedErrorMessage, expectedErrorCode);

        await GivenAggregatedMeasureDataRequestRejectedIsReceived(
            requestCalculatedEnergyTimeSeriesRejected);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should()
                .BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should()
                .ContainSingle("because there should only be one rejected message regardless of grid areas")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new(
                BusinessReason.BalanceFixing,
                "5790001330552",
                delegatedToActor.ActorNumber.Value,
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                ReasonCode.FullyRejected.Code,
                TransactionId.From("12356478912356478912356478912356478"),
                expectedErrorCode,
                expectedErrorMessage));
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(
        nameof(DocumentFormatsWithRoleCombinationsForNullGridArea),
        MemberType = typeof(GivenAggregatedMeasureDataV2RequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_AndGiven_OriginalActorRequestsOwnDataWithDataInTwoGridAreas_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesTwoNotifyAggregatedMeasureDataDocumentWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider
                            || delegatedFromRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: delegatedFromRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var gridAreaWithDelegation = "543";
        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaWithDelegation,
            ProcessType.RequestEnergyResults,
            GetNow());

        // Act
        // Original actor requests own data
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[] { (null, transactionId), });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                originalActor.ActorNumber.Value,
                originalActor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber?.Value,
                balanceResponsibleParty?.Value,
                Array.Empty<string>(),
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = new List<string>() { "542", "543" };
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput, defaultGridAreas);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            delegatedActorPeekResults.Should()
                .BeEmpty("because delegated actor shouldn't receive result when original actor made the request");
            originalActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            originalActorPeekResults,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: originalActor.ActorNumber,
                SenderId: DataHubDetails.DataHubActorNumber,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                GridAreaCode: testMessageData.ExampleMessageData.GridArea,
                OriginalTransactionIdReference: transactionId,
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                CalculationVersion: testMessageData.ExampleMessageData.Version,
                Resolution: testMessageData.ExampleMessageData.Resolution,
                Period: new Period(
                    CreateDateInstant(2022, 01, 12),
                    CreateDateInstant(2022, 01, 13)),
                Points: testMessageData.ExampleMessageData.Points));
    }

    [Theory]
    [InlineData("Xml", "MeteredDataResponsible", "Delegated")]
    [InlineData("Json", "MeteredDataResponsible", "Delegated")]
    [InlineData("Xml", "EnergySupplier", "Delegated")]
    [InlineData("Json", "EnergySupplier", "Delegated")]
    [InlineData("Xml", "BalanceResponsibleParty", "Delegated")]
    [InlineData("Json", "BalanceResponsibleParty", "Delegated")]
    public async Task AndGiven_RequestDoesNotContainOriginalActorNumber_When_DelegatedActorPeeksAllMessages_Then_DelegationIsUnsuccessfulSoRequestIsRejectedWithCorrectInvalidRoleError(
            string incomingDocumentFormatName,
            string delegatedFromRole,
            string delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = delegatedFromRole == ActorRole.EnergySupplier.Name
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : delegatedFromRole == ActorRole.BalanceResponsibleParty.Name
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = delegatedFromRole == ActorRole.EnergySupplier.Name
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = delegatedFromRole == ActorRole.BalanceResponsibleParty.Name
            ? testMessageData.ActorNumber
            : testMessageData.ExampleMessageData.BalanceResponsible;
        var gridAreaOwner = delegatedFromRole == ActorRole.GridAccessProvider.Name
                            || delegatedFromRole == ActorRole.MeteredDataResponsible.Name
            ? testMessageData.ActorNumber
            : ActorNumber.Create("5555555555555");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"),
            ActorRole: ActorRole.FromName(delegatedToRole));
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier.Name
            ? energySupplierNumber!
            : delegatedFromRole == ActorRole.BalanceResponsibleParty.Name
                ? balanceResponsibleParty!
                : gridAreaOwner, ActorRole: ActorRole.FromName(delegatedFromRole));

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var gridAreaWithDelegation = "543";
        if (originalActor.ActorRole == ActorRole.GridAccessProvider)
            await GivenGridAreaOwnershipAsync(gridAreaWithDelegation, originalActor.ActorNumber);

        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaWithDelegation,
            ProcessType.RequestEnergyResults,
            GetNow());

        var response = await GivenReceivedAggregatedMeasureDataRequest(
            DocumentFormat.FromName(incomingDocumentFormatName),
            delegatedToActor.ActorNumber,
            originalActor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 2, 1),
            periodEnd: (2022, 1, 1),
            null,
            null,
            new (string? GridArea, TransactionId TransactionId)[] { (null, transactionId), },
            assertRequestWasSuccessful: false);

        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue("because a synchronous error should have occurred");
        response.MessageBody.Should().ContainAll("The authenticated user does not hold the required role");
        senderSpy.MessageSent.Should().BeFalse();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InsertAggregatedMeasureDataDatabricksDataAsync(_ediDatabricksOptions);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private Task GivenAggregatedMeasureDataRequestRejectedIsReceived(
        Guid processId,
        AggregatedTimeSeriesRequestRejected rejectedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(AggregatedTimeSeriesRequestRejected),
            eventPayload: rejectedMessage,
            processId: processId);
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML
