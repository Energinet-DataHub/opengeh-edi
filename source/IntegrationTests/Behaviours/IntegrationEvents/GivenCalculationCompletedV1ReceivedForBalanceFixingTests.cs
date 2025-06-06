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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.NotifyAggregatedMeasureData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenCalculationCompletedV1ReceivedForBalanceFixingTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenCalculationCompletedV1ReceivedForBalanceFixingTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
            : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InsertAggregatedMeasureDataDatabricksDataAsync(_ediDatabricksOptions);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueEnergyResultsPerGridArea_When_MeteredDataResponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerGridArea();
        var testMessageData = testDataDescription.ExampleEnergyResultMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var meteredDataResponsible = new Actor(ActorNumber.Create("1111111111111"), ActorRole.MeteredDataResponsible);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCodes.Single(), meteredDataResponsible.ActorNumber);
        await GivenEnqueueEnergyResultsPerGridAreaAsync(testDataDescription.CalculationId, testDataDescription.GridAreaOwners);

        // When (act)
        var peekResultsForMeteredDataResponsible = await WhenActorPeeksAllMessages(
            meteredDataResponsible.ActorNumber,
            meteredDataResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForMeteredDataResponsible.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForGridOwnerCount);

        var assertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: meteredDataResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                                                           // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: null,
            BalanceResponsibleNumber: null,
            SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            GridAreaCode: testMessageData.ExampleMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: testMessageData.ExampleMessageData.Version,
            Resolution: testMessageData.ExampleMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.ExampleMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForMeteredDataResponsible,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsPerBalanceResponsible_When_BalanceResponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerBalanceResponsible();
        var testMessageData = testDataDescription.ExampleBalanceResponsible.ExampleMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var balanceResponsible = new Actor(
            testDataDescription.ExampleBalanceResponsible.ActorNumber,
            ActorRole.BalanceResponsibleParty);

        await GivenEnqueueEnergyResultsPerBalanceResponsible(testDataDescription.CalculationId, new Dictionary<string, ActorNumber>() { { "543", ActorNumber.Create("8500000000502") } });

        // When (act)
        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForBalanceResponsible.Should().HaveCount(testDataDescription.ExampleBalanceResponsible.ExpectedOutgoingMessagesCount * testDataDescription.GridAreaCodes.Count);

        var assertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: balanceResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                                                           // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: null,
            BalanceResponsibleNumber: balanceResponsible.ActorNumber,
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridAreaCode: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible_When_EnergySupplierAndBalanceReponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var energySupplierTestMessageData = testDataDescription.ExampleEnergySupplier.ExampleMessageData;
        var balanceResponsibleTestMessageData = testDataDescription.ExampleBalanceResponsible.ExampleMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var energySupplier = new Actor(testDataDescription.ExampleEnergySupplier.ActorNumber, ActorRole.EnergySupplier);
        var balanceResponsible = new Actor(testDataDescription.ExampleBalanceResponsible.ActorNumber, ActorRole.BalanceResponsibleParty);

        await GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(testDataDescription.CalculationId, new Dictionary<string, ActorNumber>() { { "543", ActorNumber.Create("8500000000502") } });

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            documentFormat);

        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)

        // Assert energy supplier peek result
        peekResultsForEnergySupplier
            .Should()
            .HaveCount(testDataDescription.ExampleEnergySupplier.ExpectedOutgoingMessagesCount);

        var energySupplierAssertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: energySupplier.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                                                           // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: energySupplier.ActorNumber,
            BalanceResponsibleNumber: null,
            SettlementMethod: energySupplierTestMessageData.SettlementMethod,
            MeteringPointType: energySupplierTestMessageData.MeteringPointType,
            GridAreaCode: energySupplierTestMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: energySupplierTestMessageData.Version,
            Resolution: energySupplierTestMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: energySupplierTestMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            energySupplierAssertionInput);

        // Assert balance responsible peek result
        peekResultsForBalanceResponsible
            .Should()
            .HaveCount(testDataDescription.ExampleBalanceResponsible.ExpectedOutgoingMessagesCount);

        var balanceResponsibleAssertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: balanceResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                                                           // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: balanceResponsibleTestMessageData.EnergySupplier,
            BalanceResponsibleNumber: balanceResponsible.ActorNumber,
            SettlementMethod: balanceResponsibleTestMessageData.SettlementMethod,
            MeteringPointType: balanceResponsibleTestMessageData.MeteringPointType,
            GridAreaCode: balanceResponsibleTestMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: balanceResponsibleTestMessageData.Version,
            Resolution: balanceResponsibleTestMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: balanceResponsibleTestMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            balanceResponsibleAssertionInput);
    }

    [Fact]
    public async Task AndGiven_EnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsibleWithAGapInPoints_When_EnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments()
    {
        // Given (arrange)
        var expectedNumberOfPeekResults = 2;
        var energySupplier = new Actor(ActorNumber.Create("5790001662234"), ActorRole.EnergySupplier);
        var calculationId = Guid.Parse("61d60f89-bbc5-4f7a-be98-6139aab1c1b2");
        var energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition = GetEnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition();
        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.DataObjectName, energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertAsync(
            energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.DataObjectName,
            [
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a8'", "'543'", "'5790001662234'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-01 23:00:00.000000'", "'39471.336'", "'kWh'", "Array('measured')"],
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a8'", "'543'", "'5790001662234'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-02 00:00:00.000000'", "'39472.336'", "'kWh'", "Array('measured')"],
                // "2022-02-02 01:00:00.000000" is missing
                // "2022-02-02 02:00:00.000000" is missing
                // "2022-02-02 03:00:00.000000" is missing
                // "2022-02-02 04:00:00.000000" is missing
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a8'", "'543'", "'5790001662234'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-02 05:00:00.000000'", "'39473.336'", "'kWh'", "Array('measured')"],
            ]);

        await GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(calculationId, new Dictionary<string, ActorNumber>() { { "543", ActorNumber.Create("8500000000502") } });

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            DocumentFormat.Json);

        // Then (assert)
        peekResultsForEnergySupplier.Should().HaveCount(expectedNumberOfPeekResults, "Fee result contains a single gap, which should result in two messages");

        // Assert first fee is correct and within expected period
        var assertForFirstBundle = new AssertNotifyAggregatedMeasureDataJsonDocument(peekResultsForEnergySupplier[0].Bundle);
        assertForFirstBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 1, 23, 0, 0),
                Instant.FromUtc(2023, 2, 2, 01, 0, 0)));
        assertForFirstBundle.HasPoints(
            [
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 1, 23, 0, 0), 39471.336m, CalculatedQuantityQuality.Measured),
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 2, 00, 0, 0),  39472.336m, CalculatedQuantityQuality.Measured),
            ]);

        // Assert second fee is correct and within expected period
        var assertForSecondBundle = new AssertNotifyAggregatedMeasureDataJsonDocument(peekResultsForEnergySupplier[1].Bundle);
        assertForSecondBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 2, 05, 0, 0),
                Instant.FromUtc(2023, 2, 2, 06, 0, 0)));
        assertForSecondBundle.HasPoints(
            [
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 2, 05, 0, 0), 39473.336m, CalculatedQuantityQuality.Measured),
            ]);
    }

    [Fact]
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerChargesWithMultipleGapsInFees_When_EnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments()
    {
        // Given (arrange)
        var expectedNumberOfPeekResults = 3;
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);
        var calculationId = Guid.Parse("61d60f89-bbc5-4f7a-be98-6139aab1c1b2");
        var energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition = GetEnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition();
        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.DataObjectName, energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertAsync(
            energyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition.DataObjectName,
            [
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a7'", "'543'", "'5790001662233'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-01 23:00:00.000000'", "'39471.336'", "'kWh'", "Array('measured')"],
                // "2022-02-02 00:00:00.000000" is missing
                // "2022-02-02 01:00:00.000000" is missing
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a7'", "'543'", "'5790001662233'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-02 02:00:00.000000'", "'39472.336'", "'kWh'", "Array('measured')"],
                // "2022-02-02 03:00:00.000000" is missing
                // "2022-02-02 04:00:00.000000" is missing
                ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'balance_fixing'", "'2023-02-01 23:00:00.000000'", "'2023-02-12 23:00:00.000000'", "'111'", "'10e4e982-91dc-4e1c-9079-514ed45a64a7'", "'543'", "'5790001662233'", "'7080000729821'", "'production'", "NULL", "'PT1H'", "'2023-02-02 05:00:00.000000'", "'39473.336'", "'kWh'", "Array('measured')"],
            ]);

        await GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(calculationId, new Dictionary<string, ActorNumber>() { { "543", ActorNumber.Create("8500000000502") } });

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            DocumentFormat.Json);

        // Then (assert)
        peekResultsForEnergySupplier.Should().HaveCount(expectedNumberOfPeekResults, "Fee result contains a single gap, which should result in two messages");

        // Assert first fee is correct and within expected period
        var assertForFirstBundle = new AssertNotifyAggregatedMeasureDataJsonDocument(peekResultsForEnergySupplier[0].Bundle);
        assertForFirstBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 1, 23, 0, 0),
                Instant.FromUtc(2023, 2, 2, 00, 0, 0)));
        assertForFirstBundle.HasPoints(
            [
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 1, 23, 0, 0), 39471.336m, CalculatedQuantityQuality.Measured),
            ]);

        // Assert second fee is correct and within expected period
        var assertForSecondBundle = new AssertNotifyAggregatedMeasureDataJsonDocument(peekResultsForEnergySupplier[1].Bundle);
        assertForSecondBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 2, 02, 0, 0),
                Instant.FromUtc(2023, 2, 2, 03, 0, 0)));
        assertForSecondBundle.HasPoints(
            [
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 2, 02, 0, 0),  39472.336m, CalculatedQuantityQuality.Measured),
            ]);

        // Assert second fee is correct and within expected period
        var assertForThirdBundle = new AssertNotifyAggregatedMeasureDataJsonDocument(peekResultsForEnergySupplier[2].Bundle);
        assertForThirdBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 2, 05, 0, 0),
                Instant.FromUtc(2023, 2, 2, 06, 0, 0)));
        assertForThirdBundle.HasPoints(
            [
                new TimeSeriesPointAssertionInput(Instant.FromUtc(2023, 2, 2, 05, 0, 0), 39473.336m, CalculatedQuantityQuality.Measured),
            ]);
    }

    private (string DataObjectName,  Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition) GetEnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaSchemaDefinition()
    {
        var query = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            _ediDatabricksOptions.Value,
            EventId.From(Guid.NewGuid()),
            Guid.NewGuid());
        return new(query.DataObjectName, query.SchemaDefinition);
    }

    private Task GivenEnqueueEnergyResultsPerGridAreaAsync(Guid calculationId, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueEnergyResultsForGridAreaOwnersActivity(
            GetService<ILogger<EnqueueEnergyResultsForGridAreaOwnersActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<EnergyResultEnumerator>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary()));
    }

    private Task GivenEnqueueEnergyResultsPerBalanceResponsible(Guid calculationId, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesActivity(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<EnergyResultEnumerator>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary()));
    }

    private Task GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(Guid calculationId, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<EnergyResultEnumerator>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary()));
    }
}
