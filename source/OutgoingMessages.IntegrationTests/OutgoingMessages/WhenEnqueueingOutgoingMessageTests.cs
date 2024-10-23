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

using Azure;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Factories;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Extensions;
using Xunit.Abstractions;
using RejectedEnergyResultMessageDtoBuilder = Energinet.DataHub.EDI.Tests.Factories.RejectedEnergyResultMessageDtoBuilder;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages;

public class WhenEnqueueingOutgoingMessageTests : OutgoingMessagesTestBase
{
    private readonly AcceptedEnergyResultMessageDtoBuilder _acceptedEnergyResultMessageDtoBuilder;
    private readonly RejectedEnergyResultMessageDtoBuilder _rejectedEnergyResultMessageDtoBuilder;
    private readonly ClockStub _clockStub;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;
    private readonly IFileStorageClient _fileStorageClient;
    private readonly WholesaleAmountPerChargeDtoBuilder _wholesaleAmountPerChargeDtoBuilder;
    private readonly EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder;
    private readonly EnergyResultPerGridAreaMessageDtoBuilder _energyResultPerGridAreaMessageDtoBuilder;

    public WhenEnqueueingOutgoingMessageTests(OutgoingMessagesTestFixture outgoingMessagesTestFixture, ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _acceptedEnergyResultMessageDtoBuilder = new AcceptedEnergyResultMessageDtoBuilder();
        _rejectedEnergyResultMessageDtoBuilder = new RejectedEnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
        _clockStub = (ClockStub)GetService<IClock>();
        _context = GetService<ActorMessageQueueContext>();
        _wholesaleAmountPerChargeDtoBuilder = new WholesaleAmountPerChargeDtoBuilder();
        _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder = new EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder();
        _energyResultPerGridAreaMessageDtoBuilder = new EnergyResultPerGridAreaMessageDtoBuilder();
    }

    [Fact]
    public async Task Outgoing_message_is_added_to_database_with_correct_values()
    {
        // Arrange
        var message = _energyResultPerGridAreaMessageDtoBuilder
            .WithMeteredDataResponsibleNumber(SampleData.GridOperatorNumber)
            .Build();

        var now = Instant.FromUtc(2024, 1, 1, 0, 0);
        _clockStub.SetCurrentInstant(now);

        // Act
        var createdOutgoingMessageId = await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        var expectedFileStorageReference = $"{SampleData.GridOperatorNumber}/{now.Year():0000}/{now.Month():00}/{now.Day():00}/{createdOutgoingMessageId:N}";

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var nullableMessageFromDatabase = await connection.QuerySingleOrDefaultAsync(sql);

        Assert.NotNull(nullableMessageFromDatabase);
        var messageFromDatabase = nullableMessageFromDatabase!;

        var expectedBundleId = await connection.QuerySingleOrDefaultAsync<Guid?>("SELECT Id FROM [dbo].Bundles");
        Assert.NotNull(expectedBundleId);

        var propertyAssertions = new Action[]
        {
            () => Assert.Equal(createdOutgoingMessageId, messageFromDatabase.Id),
            () => Assert.NotNull(messageFromDatabase.RecordId),
            () => Assert.Equal(message.ProcessId, messageFromDatabase.ProcessId),
            () => Assert.Equal(DocumentType.NotifyAggregatedMeasureData.Name, messageFromDatabase.DocumentType),
            () => Assert.Equal(message.ReceiverNumber.Value, messageFromDatabase.ReceiverNumber),
            () => Assert.Equal(message.ReceiverRole.Code, messageFromDatabase.ReceiverRole),
            () => Assert.Equal(message.ReceiverNumber.Value, messageFromDatabase.DocumentReceiverNumber),
            () => Assert.Equal(message.ReceiverRole.Code, messageFromDatabase.DocumentReceiverRole),
            () => Assert.Equal(DataHubDetails.DataHubActorNumber.Value, messageFromDatabase.SenderId),
            () => Assert.Equal(ActorRole.MeteredDataAdministrator.Code, messageFromDatabase.SenderRole),
            () => Assert.Equal(message.BusinessReason, messageFromDatabase.BusinessReason),
            () => Assert.Equal(message.Series.GridAreaCode, messageFromDatabase.GridAreaCode),
            () => Assert.Equal(ProcessType.ReceiveEnergyResults.Name, messageFromDatabase.MessageCreatedFromProcess),
            () => Assert.Equal(expectedFileStorageReference, messageFromDatabase.FileStorageReference),
            () => Assert.Equal("OutgoingMessage", messageFromDatabase.Discriminator),
            () => Assert.Equal(message.RelatedToMessageId?.Value, messageFromDatabase.RelatedToMessageId),
            () => Assert.Equal(message.EventId.Value, messageFromDatabase.EventId),
            () => Assert.Equal(messageFromDatabase.AssignedBundleId, expectedBundleId!),
            () => Assert.Equal(messageFromDatabase.CreatedAt, now.ToDateTimeUtc()),
            () => Assert.NotNull(messageFromDatabase.CreatedBy),
            () => Assert.Null(messageFromDatabase.ModifiedAt),
            () => Assert.Null(messageFromDatabase.ModifiedBy),
            () => Assert.Equal(message.ExternalId.Value, messageFromDatabase.ExternalId),
            () => Assert.Equal(message.CalculationId, messageFromDatabase.CalculationId),
            () => Assert.NotNull(messageFromDatabase.PeriodStartedAt),
        };

        Assert.Multiple(propertyAssertions);

        // Confirm that all database columns are asserted
        var databaseColumnsCount = ((IDictionary<string, object>)messageFromDatabase).Count;
        var propertiesAssertedCount = propertyAssertions.Length;
        propertiesAssertedCount
            .Should()
            .Be(databaseColumnsCount, "asserted properties count should be equal to OutgoingMessage database columns count");
    }

    [Fact]
    public async Task Bundle_is_added_to_database_with_correct_values()
    {
        // Arrange
        var message = _acceptedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        var now = Instant.FromUtc(2024, 1, 1, 0, 0);
        _clockStub.SetCurrentInstant(now);

        // Act
        await EnqueueAndCommitAsync(message);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[Bundles]";
        var nullableBundleFromDatabase = await connection.QuerySingleOrDefaultAsync(sql);

        Assert.NotNull(nullableBundleFromDatabase);
        var bundleFromDatabase = nullableBundleFromDatabase!;

        var expectedActorMessageQueueId = await connection.QuerySingleOrDefaultAsync<Guid?>("SELECT Id FROM [dbo].ActorMessageQueues");
        Assert.NotNull(expectedActorMessageQueueId);

        var propertyAssertions = new Action[]
        {
            () => Assert.NotNull(bundleFromDatabase.RecordId),
            () => Assert.NotNull(bundleFromDatabase.Id),
            () => Assert.Equal(bundleFromDatabase.ActorMessageQueueId, expectedActorMessageQueueId),
            () => Assert.Equal(DocumentType.NotifyAggregatedMeasureData.Name, bundleFromDatabase.DocumentTypeInBundle),
            () => Assert.Equal(1, bundleFromDatabase.MessageCount),
            () => Assert.Equal(1, bundleFromDatabase.MaxMessageCount),
            () => Assert.Equal(message.BusinessReason, bundleFromDatabase.BusinessReason),
            () => Assert.Equal(bundleFromDatabase.Created, now.ToDateTimeUtc()),
            () => Assert.Null(bundleFromDatabase.RelatedToMessageId),
            () => Assert.NotNull(bundleFromDatabase.MessageId),
            () => Assert.Null(bundleFromDatabase.DequeuedAt),
            () => Assert.Equal(bundleFromDatabase.ClosedAt, now.ToDateTimeUtc()),
            () => Assert.Null(bundleFromDatabase.PeekedAt),
            () => Assert.Equal(DocumentType.NotifyAggregatedMeasureData.Category.Name, bundleFromDatabase.MessageCategory),
        };

        Assert.Multiple(propertyAssertions);

        // Confirm that all database columns are asserted
        var databaseColumnsCount = ((IDictionary<string, object>)bundleFromDatabase).Count;
        var propertiesAssertedCount = propertyAssertions.Length;
        propertiesAssertedCount
            .Should()
            .Be(databaseColumnsCount, "asserted properties count should be equal to Bundle database columns count");
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = _acceptedEnergyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        ClearDbContextCaches();
        var result = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                message.ReceiverNumber,
                MessageCategory.Aggregations,
                message.ReceiverRole,
                DocumentFormat.Xml),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Can_peek_oldest_bundle()
    {
        var message = _acceptedEnergyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        _clockStub.SetCurrentInstant(_clockStub.GetCurrentInstant().PlusSeconds(1));
        var message2 = _acceptedEnergyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message2);

        ClearDbContextCaches();
        var result = await _outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(message.ReceiverNumber, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Ebix), CancellationToken.None);

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT top 1 MessageId FROM [dbo].[Bundles] order by created";
        var id = await
            connection
                .QuerySingleOrDefaultAsync<string>(sql);

        Assert.Equal(result!.MessageId.Value, id);
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var message = _acceptedEnergyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        ClearDbContextCaches();
        var peekRequestDto = new PeekRequestDto(
            message.ReceiverNumber,
            MessageCategory.Aggregations,
            message.ReceiverRole,
            DocumentFormat.Xml);
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(peekRequestDto, CancellationToken.None);

        var dequeueCommand = new DequeueRequestDto(
            peekResult!.MessageId.Value,
            message.ReceiverRole,
            message.ReceiverNumber);

        ClearDbContextCaches();
        var result = await _outgoingMessagesClient.DequeueAndCommitAsync(dequeueCommand, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact(Skip = "Bundling is deactivated")]
    public async Task Outgoing_messages_for_same_actor_is_added_to_existing_bundle()
    {
        // Arrange
        var actorMessageQueueId = Guid.NewGuid();
        var existingBundleId = Guid.NewGuid();
        var message = _acceptedEnergyResultMessageDtoBuilder
            .Build();
        await CreateActorMessageQueueInDatabase(actorMessageQueueId, message.ReceiverNumber, message.ReceiverRole);
        await CreateBundleInDatabase(existingBundleId, actorMessageQueueId, message.DocumentType, message.BusinessReason);

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var actualBundleId = await GetOutgoingMessageBundleIdFromDatabase(createdId);

        actualBundleId.Should().Be(existingBundleId);
    }

    [Fact]
    public async Task Outgoing_message_record_is_added_to_file_storage_with_correct_content()
    {
        // Arrange
        var serializer = new Serializer();
        var message = _acceptedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        var outgoingMessage = OutgoingMessageFactory.CreateMessage(
            message,
            serializer,
            Instant.FromUtc(2024, 1, 1, 0, 0));
        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        using var assertionScope = new AssertionScope();
        var fileStorageReference = await GetOutgoingMessageFileStorageReferenceFromDatabase(createdId);
        fileStorageReference.Should().NotBeNull();

        var fileContent = await GetFileContentFromFileStorageAsync("outgoing", fileStorageReference!);
        fileContent.Should().Be(outgoingMessage.GetSerializedContent());
    }

    [Fact]
    public async Task Uploading_duplicate_outgoing_message_to_file_storage_throws_exception()
    {
        // Arrange
        var message = _acceptedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var fileStorageReference = await GetOutgoingMessageFileStorageReferenceFromDatabase(createdId);
        var uploadDuplicateFile = async () => await _fileStorageClient.UploadAsync(new FileStorageReference(OutgoingMessage.FileStorageCategory, fileStorageReference!), new MemoryStream(new byte[] { 0x20 }));

        (await uploadDuplicateFile.Should().ThrowAsync<RequestFailedException>())
            .And.ErrorCode.Should().Be("BlobAlreadyExists");
    }

    [Fact]
    public async Task Enqueuing_multiple_outgoing_messages_assigns_them_to_different_bundles()
    {
        // Arrange
        var message1 = _rejectedEnergyResultMessageDtoBuilder
            .Build();
        var message2 = _rejectedEnergyResultMessageDtoBuilder
            .Build();
        var message3 = _rejectedEnergyResultMessageDtoBuilder
            .Build();

        // Act
        var createdIdMessage1 = await EnqueueAndCommitAsync(message1);
        var createdIdMessage2 = await EnqueueAndCommitAsync(message2);
        var createdIdMessage3 = await EnqueueAndCommitAsync(message3);

        // Assert
        var bundleIdForMessage1 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage1);
        var bundleIdForMessage2 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage2);
        var bundleIdForMessage3 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage3);

        Assert.NotEqual(bundleIdForMessage1, bundleIdForMessage2);
        Assert.NotEqual(bundleIdForMessage1, bundleIdForMessage3);
        Assert.NotEqual(bundleIdForMessage2, bundleIdForMessage3);
    }

    [Fact(Skip = "Bundling is deactivated")]
    public async Task Outgoing_messages_with_different_relatedTo_ids_are_assigned_to_different_bundles()
    {
        // Arrange
        var actorMessageQueueId = Guid.NewGuid();
        var existingBundleId = Guid.NewGuid();
        var receiverId = ActorNumber.Create("1234567891912");
        var receiverRole = ActorRole.MeteredDataAdministrator;
        var maxMessageCount = 3;
        var message1RelatedTo = MessageId.New();
        var message2RelatedTo = MessageId.New();
        var message3RelatedTo = MessageId.New();

        var message1 = _rejectedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(receiverId.Value)
            .WithReceiverRole(receiverRole)
            .WithRelationTo(message1RelatedTo)
            .Build();
        var message2 = _rejectedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(receiverId.Value)
            .WithReceiverRole(receiverRole)
            .WithRelationTo(message2RelatedTo)
            .Build();
        var message3 = _rejectedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(receiverId.Value)
            .WithReceiverRole(receiverRole)
            .WithRelationTo(message3RelatedTo)
            .Build();

        // We have to manually create the queue and bundle to ensure that the bundle has a maxMessageCount
        // such that we do not close the bundle when we enqueue the first message
        await CreateActorMessageQueueInDatabase(actorMessageQueueId, receiverId, receiverRole);
        await CreateBundleInDatabase(
            existingBundleId,
            actorMessageQueueId,
            message1.DocumentType,
            message1.BusinessReason,
            maxMessageCount,
            message1RelatedTo);

        // Act
        var createdIdMessage1 = await EnqueueAndCommitAsync(message1);
        var createdIdMessage2 = await EnqueueAndCommitAsync(message2);
        var createdIdMessage3 = await EnqueueAndCommitAsync(message3);

        // Assert
        var bundleIdForMessage1 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage1);
        var bundleIdForMessage2 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage2);
        var bundleIdForMessage3 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage3);

        Assert.Equal(existingBundleId, bundleIdForMessage1); // This is not correct as long as bundling is disabled
        Assert.NotEqual(existingBundleId, bundleIdForMessage2);
        Assert.NotEqual(existingBundleId, bundleIdForMessage3);

        Assert.NotEqual(bundleIdForMessage2, bundleIdForMessage3);
    }

    [Fact(Skip = "Bundling is deactivated")]
    public async Task Outgoing_messages_with_same_relatedTo_ids_are_assigned_to_same_bundles()
    {
        // Arrange
        var actorMessageQueueId = Guid.NewGuid();
        var existingBundleId = Guid.NewGuid();
        var receiverId = ActorNumber.Create("1234567891912");
        var receiverRole = ActorRole.MeteredDataAdministrator;
        var maxMessageCount = 2;
        var messageRelatedTo = MessageId.New();

        var message = _rejectedEnergyResultMessageDtoBuilder
            .WithReceiverNumber(receiverId.Value)
            .WithReceiverRole(receiverRole)
            .WithRelationTo(messageRelatedTo)
            .Build();

        // We have to manually create the queue and bundle to ensure that the bundle has a maxMessageCount
        // such that we do not close the bundle when we enqueue the first message
        await CreateActorMessageQueueInDatabase(actorMessageQueueId, receiverId, receiverRole);
        await CreateBundleInDatabase(
            existingBundleId,
            actorMessageQueueId,
            message.DocumentType,
            message.BusinessReason,
            maxMessageCount,
            messageRelatedTo);

        // Act
        var createdIdMessage1 = await EnqueueAndCommitAsync(message);
        var createdIdMessage2 = await EnqueueAndCommitAsync(message);

        // Assert
        var bundleIdForMessage1 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage1);
        var bundleIdForMessage2 = await GetOutgoingMessageBundleIdFromDatabase(createdIdMessage2);

        Assert.Equal(existingBundleId, bundleIdForMessage1);
        Assert.Equal(existingBundleId, bundleIdForMessage2);
    }

    /// <summary>
    /// This test verifies the "hack" for a MDR/GridOperator actor which is the same Actor but with two distinct roles MDR and GridOperator
    /// The actor uses the MDR (MeteredDataResponsible) role when making request (RequestAggregatedMeasureData)
    /// but uses the DDM (GridOperator) role when peeking.
    /// This means that a NotifyAggregatedMeasureData document with a MDR receiver should be added to the DDM ActorMessageQueue
    /// TODO: This test should be updated to use our new energy results dto's (EnergyResultPerGridAreaMessageDto etc.)
    /// </summary>
    [Fact]
    public async Task Given_EnqueuingNotifyAggregatedMeasureData_When_ReceiverActorRoleIsMDR_Then_MessageShouldBeEnqueuedAsDDM()
    {
        // Arrange
        var message = _acceptedEnergyResultMessageDtoBuilder
            .WithReceiverRole(ActorRole.MeteredDataResponsible)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var fromDb = await GetOutgoingMessageWithActorMessageQueueFromDatabase(createdId);

        fromDb.ActorMessageQueueNumber.Should().Be(message.ReceiverNumber.Value);
        fromDb.ActorMessageQueueRole.Should().Be(ActorRole.GridAccessProvider.Code);
        fromDb.OutgoingMessageReceiverRole.Should().Be(ActorRole.MeteredDataResponsible.Code);
    }

    [Theory]
    [InlineData("WholesaleFixing")]
    [InlineData("Correction")]
    public async Task Given_EnqueuingEnergyResultsFromWholesaleFixingAndCorrections_When_ReceiverActorRoleIsDDK_Then_MessageShouldBeEmpty(string businessReasonName)
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var externalId = Guid.NewGuid();
        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(receiverId.Value)
            .WithBalanceResponsiblePartyReceiverNumber(receiverId.Value)
            .WithBusinessReason(BusinessReason.FromName(businessReasonName))
            .WithCalculationResultId(externalId)
            .Build();

        // Act
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        var messages = await GetOutgoingMessagesFor(receiverId, ActorRole.BalanceResponsibleParty);

        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Outgoing_message_must_only_be_enqueued_once_to_an_receiver_with_same_event_id_and_period()
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var eventId = Guid.NewGuid();

        var message = _wholesaleAmountPerChargeDtoBuilder
            .WithReceiverNumber(receiverId)
            .WithEventId(eventId)
            .WithPeriod(new(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()))
            .Build();

        var countBeforeEnqueue = await GetCountOfOutgoingMessagesFromDatabase();

        // Act
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        var countAfterEnqueue = await GetCountOfOutgoingMessagesFromDatabase();

        // No messages are in the database before enqueue is called
        countBeforeEnqueue.Should().Be(0);
        // Only two message should be in the database after enqueue is called twice one for the energy supplier and one for the charge owner
        countAfterEnqueue.Should().Be(2);
    }

    [Fact]
    public async Task Outgoing_message_can_be_enqueued_multiple_times_to_same_receiver_with_different_roles_and_same_eventId()
    {
        // Arrange
        var receiverId = ActorNumber.Create("1234567891912");
        var eventId = EventId.From(Guid.NewGuid());

        var message = _energyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
            .WithEnergySupplierReceiverNumber(receiverId.Value)
            .WithBalanceResponsiblePartyReceiverNumber(receiverId.Value)
            .WithEventId(eventId)
            .Build();

        var countBeforeEnqueue = await GetCountOfOutgoingMessagesFromDatabase();

        // Act
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);

        // Assert
        var countAfterEnqueue = await GetCountOfOutgoingMessagesFromDatabase();

        // No messages are in the database before enqueue is called
        countBeforeEnqueue.Should().Be(0);
        // Only two message should be in the database after enqueue is called twice, one message for the energy supplier and one for the balance responsible.
        countAfterEnqueue.Should().Be(2);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private async Task<(string ActorMessageQueueNumber, string ActorMessageQueueRole, string OutgoingMessageReceiverRole)> GetOutgoingMessageWithActorMessageQueueFromDatabase(Guid createdId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var result = await connection.QuerySingleAsync(
            @"SELECT tQueue.ActorNumber, tQueue.ActorRole, tOutgoing.ReceiverRole FROM [dbo].[OutgoingMessages] AS tOutgoing
                    INNER JOIN [dbo].[Bundles] as tBundle ON tOutgoing.AssignedBundleId = tBundle.Id
                    INNER JOIN [dbo].ActorMessageQueues as tQueue on tBundle.ActorMessageQueueId = tQueue.Id",
            new
            {
                Id = createdId.ToString(),
            });

        return (ActorMessageQueueNumber: result.ActorNumber, ActorMessageQueueRole: result.ActorRole, OutgoingMessageReceiverRole: result.ReceiverRole);
    }

    private async Task<List<dynamic>> GetOutgoingMessagesFor(ActorNumber actorNumber, ActorRole actorRole)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var messages = await connection.QueryAsync(
            @"SELECT outgoing.Id FROM [dbo].[OutgoingMessages] AS outgoing
                        WHERE outgoing.ReceiverNumber = @ReceiverNumber AND outgoing.ReceiverRole = @ReceiverRole",
            new
            {
                ReceiverNumber = actorNumber.Value,
                ReceiverRole = actorRole.Code,
            });

        return messages.ToList();
    }

    private async Task<string?> GetOutgoingMessageFileStorageReferenceFromDatabase(Guid id)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[OutgoingMessages] WHERE Id = '{id}'");

        return fileStorageReference;
    }

    private async Task<Guid> GetOutgoingMessageBundleIdFromDatabase(Guid id)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var assignedBundleId = await connection.ExecuteScalarAsync<Guid>($"SELECT AssignedBundleId FROM [dbo].[OutgoingMessages] WHERE Id = '{id}'");

        return assignedBundleId;
    }

    private async Task<int> GetCountOfOutgoingMessagesFromDatabase()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfMessages = await connection
            .ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OutgoingMessages");
        return numberOfMessages;
    }

    private async Task<Guid> EnqueueAndCommitAsync(AcceptedEnergyResultMessageDto message)
    {
        var outgoingMessageId = await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        await actorMessageQueueContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return outgoingMessageId;
    }

    private async Task<Guid> EnqueueAndCommitAsync(RejectedEnergyResultMessageDto message)
    {
        var outgoingMessageId = await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        await actorMessageQueueContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return outgoingMessageId;
    }

    private async Task CreateActorMessageQueueInDatabase(Guid id, ActorNumber actorNumber, ActorRole actorRole)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var clock = GetService<IClock>();

        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[ActorMessageQueues] (Id, ActorNumber, ActorRole, CreatedBy, CreatedAt)
                    VALUES (@Id, @ActorNumber, @ActorRole, @CreatedBy, @CreatedAt)",
            new
            {
                Id = id,
                ActorNumber = actorNumber.Value,
                ActorRole = actorRole.Code,
                CreatedBy = "Test",
                CreatedAt = clock.GetCurrentInstant(),
            });
    }

    private async Task CreateBundleInDatabase(Guid id, Guid actorMessageQueueId, DocumentType documentType, string businessReason, int? maxMessageCount = null, MessageId? relatedToMessageId = null)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[Bundles] (Id, MessageId, ActorMessageQueueId, DocumentTypeInBundle, MessageCount, MaxMessageCount, BusinessReason, Created, RelatedToMessageId)
                    VALUES (@Id, @MessageId, @ActorMessageQueueId, @DocumentTypeInBundle, @MessageCount, @MaxMessageCount, @BusinessReason, @Created, @RelatedToMessageId)",
            new
            {
                Id = id,
                MessageId = id.ToString("N"),
                ActorMessageQueueId = actorMessageQueueId,
                DocumentTypeInBundle = documentType.Name,
                MessageCount = 0,
                MaxMessageCount = maxMessageCount ?? 1,
                BusinessReason = businessReason,
                Created = new DateTime(2022, 2, 2),
                RelatedToMessageId = relatedToMessageId?.Value,
            });
    }
}
