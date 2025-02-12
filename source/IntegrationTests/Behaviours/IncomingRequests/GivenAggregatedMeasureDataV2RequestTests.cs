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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenAggregatedMeasureDataV2RequestTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenAggregatedMeasureDataV2RequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration, true);
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public static object[][] DocumentFormatsWithActorRoleCombinationsForNullGridArea() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: true);

    public static object[][] DocumentFormatsWithAllActorRoleCombinations() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: false);

    public static object[][] DocumentFormatsWithActorRoleCombinations(bool nullGridArea)
    {
        // The actor roles who can perform AggregatedMeasureDataRequest's
        var actorRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.BalanceResponsibleParty,
        };

        if (!nullGridArea)
        {
            actorRoles.Add(ActorRole.MeteredDataResponsible);
            actorRoles.Add(ActorRole.GridAccessProvider); // Grid Operator can make requests because of DDM -> MDR hack
        }

        var incomingDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(except: new[]
            {
                DocumentFormat.Ebix.Name, // ebIX is not supported for requests
            })
            .ToArray();

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return actorRoles
            .SelectMany(actorRole => incomingDocumentFormats
                .SelectMany(incomingDocumentFormat => peekDocumentFormats
                    .Select(peekDocumentFormat => new object[]
                    {
                        actorRole,
                        incomingDocumentFormat,
                        peekDocumentFormat,
                    })))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyAggregatedMeasureDataDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var testMessageData = actorRole == ActorRole.EnergySupplier
             ? testDataDescription.ExampleEnergySupplier
             : testDataDescription.ExampleBalanceResponsible;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = testMessageData.ExampleMessageData.BalanceResponsible;
        var actor = (ActorNumber: testMessageData.ActorNumber, ActorRole: actorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (testMessageData.ExampleMessageData.GridArea, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber!.Value,
                balanceResponsibleParty!.Value,
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
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle("because there should be one message when requesting for one grid area")
                .Subject;
        }

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: actor.ActorNumber,
                SenderId: DataHubDetails.DataHubActorNumber,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                GridAreaCode: testMessageData.ExampleMessageData.GridArea,
                OriginalTransactionIdReference: transactionId,
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: testMessageData.ExampleMessageData.Version,
                Resolution: testMessageData.ExampleMessageData.Resolution,
                Period: new Period(
                    CreateDateInstant(2022, 01, 12),
                    CreateDateInstant(2022, 01, 13)),
                Points: testMessageData.ExampleMessageData.Points));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinationsForNullGridArea))]
    public async Task AndGiven_DataInTwoGridAreas_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyAggregatedMeasureDataDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var testMessageData = actorRole == ActorRole.EnergySupplier
            ? testDataDescription.ExampleEnergySupplier
            : testDataDescription.ExampleBalanceResponsible;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = testMessageData.ExampleMessageData.BalanceResponsible;
        var actor = (ActorNumber: testMessageData.ActorNumber, ActorRole: actorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber!.Value,
                balanceResponsibleParty!.Value,
                new List<string>(),
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = testDataDescription.GridAreaCodes;
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput, defaultGridAreas);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            peekResults.Should().HaveSameCount(defaultGridAreas, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in peekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyAggregatedMeasureDataDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.BalanceFixing,
                        null),
                    ReceiverId: actor.ActorNumber,
                    SenderId: DataHubDetails.DataHubActorNumber,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                    MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                    GridAreaCode: peekResultGridArea,
                    OriginalTransactionIdReference: transactionId,
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: testMessageData.ExampleMessageData.Version,
                    Resolution: testMessageData.ExampleMessageData.Resolution,
                    Period: new Period(
                        CreateDateInstant(2022, 01, 12),
                        CreateDateInstant(2022, 01, 13)),
                    Points: testMessageData.ExampleMessageData.GridArea == peekResultGridArea ? testMessageData.ExampleMessageData.Points : null));
        }

        resultGridAreas.Should().BeEquivalentTo("542", "543");
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_RequestHasNoDataInOptionalFields_When_ActorPeeksAllMessages_Then_ReceivesNotifyAggregatedMeasureDataDocumentWithCorrectContent(
            ActorRole actorRole,
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testMessageData = actorRole == ActorRole.EnergySupplier
            ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().ExampleEnergySupplier
            : actorRole == ActorRole.BalanceResponsibleParty
                ? GivenDatabricksResultDataForEnergyResultPerBalanceResponsible().ExampleBalanceResponsible
                : GivenDatabricksResultDataForEnergyResultPerGridArea().ExampleEnergyResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierOrNull = actorRole == ActorRole.EnergySupplier
            ? testMessageData.ExampleMessageData.EnergySupplier
            : null;
        var balanceResponsibleOrNull = actorRole == ActorRole.BalanceResponsibleParty
            ? testMessageData.ExampleMessageData.BalanceResponsible
            : null;
        var actor = (ActorNumber: testMessageData.ActorNumber, ActorRole: actorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var gridAreaOrNull = actor.ActorRole == ActorRole.GridAccessProvider || actor.ActorRole == ActorRole.MeteredDataResponsible
            ? testMessageData.ExampleMessageData.GridArea
            : null;

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            meteringPointType: null,
            settlementMethod: null,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierOrNull,
            balanceResponsibleParty: balanceResponsibleOrNull,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaOrNull, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierOrNull?.Value,
                balanceResponsibleOrNull?.Value,
                GridAreas: gridAreaOrNull != null ? [gridAreaOrNull] : new List<string>(),
                SettlementMethod: null,
                MeteringPointType: null,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = gridAreaOrNull != null
            ? null
            : actorRole == ActorRole.EnergySupplier
                ? GivenDatabricksResultDataForEnergyResultPerEnergySupplier().GridAreaCodes
                : new List<string> { testMessageData.ExampleMessageData.GridArea };
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput, defaultGridAreas);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert

        // Assert
        using (new AssertionScope())
        {
            peekResults.Count.Should().Be(testMessageData.ExpectedOutgoingMessagesCount, "because there should be one message for each grid area, metering point type and settlement method combination");
        }

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResults,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: actor.ActorNumber,
                SenderId: DataHubDetails.DataHubActorNumber,
                EnergySupplierNumber: energySupplierOrNull,
                BalanceResponsibleNumber: balanceResponsibleOrNull,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                GridAreaCode: testMessageData.ExampleMessageData.GridArea,
                OriginalTransactionIdReference: transactionId,
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: testMessageData.ExampleMessageData.Version,
                Resolution: testMessageData.ExampleMessageData.Resolution,
                Period: new Period(
                    CreateDateInstant(2022, 01, 12),
                    CreateDateInstant(2022, 01, 13)),
                Points: testMessageData.ExampleMessageData.Points));
    }

    public async Task InitializeAsync()
    {
        await _fixture.InsertAggregatedMeasureDataDatabricksDataAsync(_ediDatabricksOptions);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
