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

using System.Reflection;
using System.Xml.Linq;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages;

public class WhenAPeekIsRequestedTests : OutgoingMessagesTestBase
{
    private readonly EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder;
    private readonly EnergyResultPerGridAreaMessageDtoBuilder _energyResultPerGridAreaMessageDtoBuilder;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ClockStub _clockStub;
    private readonly IUnitOfWork _unitOfWork;

    public WhenAPeekIsRequestedTests(OutgoingMessagesTestFixture outgoingMessagesTestFixture, ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder = new EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder();
        _energyResultPerGridAreaMessageDtoBuilder = new EnergyResultPerGridAreaMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _unitOfWork = GetService<IUnitOfWork>();
        _clockStub = (ClockStub)GetService<IClock>();
    }

    public static TheoryData<DocumentFormat> GetAllDocumentFormat => new TheoryData<DocumentFormat>(
        EnumerationType.GetAll<DocumentFormat>().ToArray());

    public static object[][] GetUnusedDataHubTypesWithDocumentFormat()
    {
        var documentFormats = EnumerationType.GetAll<DocumentFormat>();
        var dataHubTypesWithUnused = Assembly.GetAssembly(typeof(DataHubTypeWithUnused<>))!
            .GetTypes()
            .Where(
                t => t.BaseType is { IsGenericType: true }
                     && t.BaseType.GetGenericTypeDefinition() == typeof(DataHubTypeWithUnused<>))
            .ToArray();

        var typesWithDocumentFormats = dataHubTypesWithUnused
            .SelectMany(t => documentFormats.Select(df => new object[] { t, df }))
            .ToArray();

        return typesWithDocumentFormats;
    }

    [Fact]
    public async Task When_no_messages_are_available_return_empty_result()
    {
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder.Build();
        await EnqueueAndCommitMessage(message);

        var result = await PeekMessageAsync(MessageCategory.None);

        Assert.Null(result);
        Assert.True(await BundleIsRegistered());
    }

    [Fact]
    public async Task A_message_bundle_is_returned()
    {
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        AssertXmlMessage.Document(XDocument.Load(result!.Bundle))
            .IsDocumentType(DocumentType.NotifyAggregatedMeasureData)
            .IsBusinessReason(BusinessReason.BalanceFixing)
            .HasSerieRecordCount(1);
    }

    [Fact]
    public async Task Ensure_same_bundle_is_returned_if_not_dequeued()
    {
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        var firstPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        var secondPeekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        Assert.NotNull(firstPeekResult);
        Assert.NotNull(secondPeekResult);

        var firstPeekContent = await GetStreamContentAsStringAsync(firstPeekResult.Bundle);
        var secondPeekContent = await GetStreamContentAsStringAsync(secondPeekResult.Bundle);
        Assert.Equal(firstPeekContent, secondPeekContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_content()
    {
        // Arrange
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        // Assert
        using var assertScope = new AssertionScope();
        peekResult.Should().NotBeNull();

        var peekResultFileContent = await GetStreamContentAsStringAsync(peekResult!.Bundle);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId.Value);
        archivedMessageFileStorageReference.Should().NotBeNull();

        var archivedMessageFileContent = await GetFileContentFromFileStorageAsync(
            "archived",
            archivedMessageFileStorageReference!);

        archivedMessageFileContent.Should().Be(peekResultFileContent);
    }

    [Fact]
    public async Task A_market_document_is_archived_with_correct_file_storage_reference()
    {
        // Arrange
        int year = 2024,
            month = 01,
            date = 02;
        _clockStub.SetCurrentInstant(Instant.FromUtc(year, month, date, 11, 07));
        var receiverNumber = SampleData.NewEnergySupplierNumber;
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(receiverNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        // Act
        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        // Assert
        result.Should().NotBeNull();

        var theIdOfArchivedMessage = await GetIdOfArchivedMessageFromDatabaseAsync(result!.MessageId.Value);
        var fileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(result.MessageId.Value);
        fileStorageReference.Should().Be($"{receiverNumber}/{year:0000}/{month:00}/{date:00}/{theIdOfArchivedMessage:N}");
    }

    [Fact]
    public async Task A_market_document_is_added_to_database()
    {
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        var result = await PeekMessageAsync(MessageCategory.Aggregations);

        result.Should().NotBeNull();

        var marketDocumentExists = await MarketDocumentExists(result!.MessageId);
        marketDocumentExists.Should().BeTrue();
    }

    [Fact]
    public async Task The_created_market_document_uses_the_archived_message_file_reference()
    {
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        await EnqueueAndCommitMessage(message);

        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations);

        var marketDocumentFileStorageReference = await GetMarketDocumentFileStorageReferenceFromDatabaseAsync(peekResult!.MessageId);
        var archivedMessageFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(peekResult.MessageId.Value);

        marketDocumentFileStorageReference.Should().Be(archivedMessageFileStorageReference);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that when peeking as a MDR we should peek the DDM queue
    /// </summary>
    [Fact]
    public async Task When_PeekingAsMeteredDataResponsible_Then_FindsGridOperatorMessages()
    {
        // Arrange
        var actorNumber = ActorNumber.Create("1234567890123");
        var message = _energyResultPerGridAreaMessageDtoBuilder
            .WithMeteredDataResponsibleNumber(actorNumber.Value)
            .Build();

        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Act
        var peekResult = await PeekMessageAsync(MessageCategory.Aggregations, actorNumber: actorNumber, actorRole: ActorRole.MeteredDataResponsible);

        // Assert
        peekResult.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetUnusedDataHubTypesWithDocumentFormat))]
    public async Task When_unused_datahub_value_then_exception_is_thrown(Type dataHubTypeWithUnused, DocumentFormat documentFormat)
    {
        ArgumentNullException.ThrowIfNull(dataHubTypeWithUnused);
        ArgumentNullException.ThrowIfNull(documentFormat);

        string unusedCode;

        var builder = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(SampleData.NewEnergySupplierNumber);

        if (dataHubTypeWithUnused == typeof(SettlementVersion))
        {
            unusedCode = "D10";
            builder.WithSettlementVersion(SettlementVersion.FromCodeOrUnused(unusedCode));
        }
        else
        {
            throw new NotImplementedException($"Type {dataHubTypeWithUnused.Name} is not implemented yet");
        }

        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder.Build();

        var act = async () =>
        {
            await EnqueueAndCommitMessage(message);

            var result = await PeekMessageAsync(MessageCategory.Aggregations, message.ReceiverNumber, message.ReceiverRole, documentFormat: documentFormat);
            return result;
        };

        (await act.Should().ThrowAsync<InvalidOperationException>($"because {unusedCode} is a unused {dataHubTypeWithUnused.Name} code"))
            .WithMessage($"{unusedCode} is not a valid {dataHubTypeWithUnused.Name}*");
    }

    [Fact]
    public async Task Given_OutgoingMessage_When_MessageIsPeeked_Then_MessageIsArchivedWithCorrectData()
    {
        // Arrange / Given
        var expectedEventId = EventId.From(Guid.NewGuid());
        var outgoingMessage = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEventId(expectedEventId)
            .Build();

        // The receiver of a EnergyResultPerEnergySupplierPerBalanceResponsibleMessage is always the balance responsible
        var receiver = (ReceiverNumber: outgoingMessage.BalanceResponsibleNumber, ReceiverRole: ActorRole.BalanceResponsibleParty);

        await EnqueueAndCommitMessage(outgoingMessage);

        var year = 2023;
        var month = 1;
        var day = 1;
        var hour = 13;
        var minute = 37;
        var expectedTimestamp = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        _clockStub.SetCurrentInstant(expectedTimestamp.ToInstant());

        // Act / When
        var peekResult = await PeekMessageAsync(
            MessageCategory.Aggregations,
            actorNumber: receiver.ReceiverNumber,
            actorRole: receiver.ReceiverRole);

        // Assert / Then
        peekResult.Should().NotBeNull("because a peek result should be found");
        peekResult!.MessageId.Should().NotBeNull("because a peek result with a message id should be found");

        var archivedMessage = await GetArchivedMessageFromDatabaseAsync(peekResult.MessageId.Value);
        ((object?)archivedMessage).Should().NotBeNull("because an archived message should exists");

        var expectedFileStorageReference = $"{receiver.ReceiverNumber.Value}/{year:0000}/{month:00}/{day:00}/{archivedMessage!.Id:N}";
        var assertProperties = new Dictionary<string, Action<object?>>
        {
            { "BusinessReason", businessReason => businessReason.Should().Be(BusinessReason.FromName(outgoingMessage.BusinessReason).Code) },
            { "CreatedAt", createdAt => createdAt.Should().Be(expectedTimestamp) },
            { "DocumentType", documentType => documentType.Should().Be(outgoingMessage.DocumentType.Name) },
            { "EventIds", eventIds => eventIds.Should().Be(expectedEventId.Value) },
            { "FileStorageReference", fileStorageReference => fileStorageReference.Should().Be(expectedFileStorageReference) },
            { "Id", id => id.Should().NotBeNull() },
            { "MessageId", messageId => messageId.Should().Be(peekResult.MessageId.Value) },
            { "ReceiverNumber", receiverNumber => receiverNumber.Should().Be(receiver.ReceiverNumber.Value) },
            { "ReceiverRoleCode", receiverRoleCode => receiverRoleCode.Should().Be(receiver.ReceiverRole.Code) },
            { "SenderRoleCode", senderRoleCode => senderRoleCode.Should().Be(ActorRole.MeteredDataAdministrator.Code) },
            { "RecordId", recordId => recordId.Should().NotBeNull() },
            { "RelatedToMessageId", relatedToMessageId => relatedToMessageId.Should().BeNull() },
            { "SenderNumber", senderNumber => senderNumber.Should().Be(DataHubDetails.DataHubActorNumber.Value) },
        };

        using var assertionScope = new AssertionScope();
        var archivedMessageAsDictionary = (IDictionary<string, object>)archivedMessage;

        foreach (var assertProperty in assertProperties)
        {
            assertProperty.Value(archivedMessageAsDictionary[assertProperty.Key]);
        }

        assertProperties.Should().HaveSameCount(archivedMessageAsDictionary, "because all archived message properties should be asserted");

        foreach (var dbPropertyName in archivedMessageAsDictionary.Keys)
        {
            assertProperties.Keys.Should().Contain(dbPropertyName);
        }
    }

    [Fact]
    public async Task Given_OutgoingMessagesForBundling_When_MessagesArePeeked_Then_PeekReturnsNothing()
    {
        // Arrange / Given
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var bundledMessage1 = new AcceptedForwardMeteredDataMessageDtoBuilder()
            .WithReceiver(receiver)
            .Build();
        var bundledMessage2 = new AcceptedForwardMeteredDataMessageDtoBuilder()
            .WithReceiver(receiver)
            .Build();

        await EnqueueAndCommitMessage(bundledMessage1);
        await EnqueueAndCommitMessage(bundledMessage2);

        // Act / When
        var peekResult = await PeekMessageAsync(
            MessageCategory.MeasureData,
            actorNumber: receiver.ActorNumber,
            actorRole: receiver.ActorRole);

        // Assert / Then
        peekResult.Should().BeNull("because the messages shouldn't be bundled yet, so no message should be peeked");
    }

    [Theory]
    [MemberData(nameof(GetAllDocumentFormat))]
    public async Task Given_EnqueuedRsm012_AndGiven_DisallowedPeekingRsm012_When_MessagesArePeekedInAnyFormat_Then_PeekReturnsNothing(DocumentFormat documentFormat)
    {
        // Arrange / Given
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.PeekMeasurementMessages, false);

        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var bundledMessage = new AcceptedForwardMeteredDataMessageDtoBuilder()
            .WithReceiver(receiver)
            .Build();
        var whenMessageIsEnqueued = Instant.FromUtc(2024, 7, 1, 14, 57, 09);
        _clockStub.SetCurrentInstant(whenMessageIsEnqueued);

        await EnqueueAndCommitMessage(bundledMessage);

        await GivenBundleMessagesHasBeenTriggered(whenMessageIsEnqueued);

        // Act / When
        var peekResult = await PeekMessageAsync(
            documentFormat == DocumentFormat.Ebix ? MessageCategory.None : MessageCategory.MeasureData,
            actorNumber: receiver.ActorNumber,
            actorRole: receiver.ActorRole,
            documentFormat: documentFormat);

        // Assert / Then
        peekResult.Should().BeNull("because the messages shouldn't be allowed to be peeked.");
    }

    [Theory]
    [MemberData(nameof(GetAllDocumentFormat))]
    public async Task Given_EnqueuedRsm012_AndGiven_AllowedPeekingRsm012_When_MessagesArePeekedInAnyFormat_Then_PeekReturnsDocument(DocumentFormat documentFormat)
    {
        // Arrange / Given
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.PeekMeasurementMessages, true);

        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var bundledMessage = new AcceptedForwardMeteredDataMessageDtoBuilder()
            .WithReceiver(receiver)
            .Build();
        var whenMessageIsEnqueued = Instant.FromUtc(2024, 7, 1, 14, 57, 09);
        _clockStub.SetCurrentInstant(whenMessageIsEnqueued);

        await EnqueueAndCommitMessage(bundledMessage);

        await GivenBundleMessagesHasBeenTriggered(whenMessageIsEnqueued);

        // Act / When
        var peekResult = await PeekMessageAsync(
            documentFormat == DocumentFormat.Ebix ? MessageCategory.None : MessageCategory.MeasureData,
            actorNumber: receiver.ActorNumber,
            actorRole: receiver.ActorRole,
            documentFormat: documentFormat);

        // Assert / Then
        peekResult.Should().NotBeNull("because the messages is allowed to be peeked.");
    }

    private async Task GivenBundleMessagesHasBeenTriggered(Instant whenMessageIsEnqueued)
    {
        var bundlingOptions = GetService<IOptions<BundlingOptions>>().Value;
        var whenBundleShouldBeClosed = whenMessageIsEnqueued.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));
        _clockStub.SetCurrentInstant(whenBundleShouldBeClosed);
        using var scope = ServiceProvider.CreateScope();
        var bundleClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);
    }

    private async Task<bool> BundleIsRegistered()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfBundles = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Bundles");
        return numberOfBundles > 0;
    }

    private async Task<bool> MarketDocumentExists(MessageId marketDocumentMessageId)
    {
        var sqlStatement =
            $"SELECT COUNT(*) "
            + $"FROM [dbo].[MarketDocuments] md JOIN [dbo].[Bundles] b ON md.BundleId = b.Id "
            + $"WHERE b.MessageId = '{marketDocumentMessageId.Value}'";

        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var exists = await connection.ExecuteScalarAsync<bool>(sqlStatement);

        return exists;
    }

    private async Task EnqueueAndCommitMessage(EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }

    private async Task EnqueueAndCommitMessage(AcceptedSendMeasurementsMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        await _unitOfWork.CommitTransactionAsync(CancellationToken.None);
    }
}
