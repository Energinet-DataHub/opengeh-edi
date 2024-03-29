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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenEnqueueingOutgoingMessageTests : TestBase
{
    private readonly EnergyResultMessageDtoBuilder _energyResultMessageDtoBuilder;
    private readonly RejectedEnergyResultMessageDtoBuilder _rejectedEnergyResultMessageDtoBuilder;
    private readonly SystemDateTimeProviderStub _systemDateTimeProvider;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;
    private readonly IFileStorageClient _fileStorageClient;

    public WhenEnqueueingOutgoingMessageTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _energyResultMessageDtoBuilder = new EnergyResultMessageDtoBuilder();
        _rejectedEnergyResultMessageDtoBuilder = new RejectedEnergyResultMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
        _systemDateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _context = GetService<ActorMessageQueueContext>();
    }

    [Fact]
    public async Task Outgoing_message_is_added_to_database_with_correct_values()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        var createdAtTimestamp = Instant.FromUtc(2024, 1, 1, 0, 0);
        _systemDateTimeProvider.SetNow(createdAtTimestamp);

        // Act
        var createdOutgoingMessageId = await EnqueueAndCommitAsync(message);

        // Assert
        var expectedFileStorageReference = $"{SampleData.NewEnergySupplierNumber}/{createdAtTimestamp.Year():0000}/{createdAtTimestamp.Month():00}/{createdAtTimestamp.Day():00}/{createdOutgoingMessageId.Value:N}";

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QuerySingleOrDefaultAsync(sql);

        Assert.NotNull(result);

        var propertyAssertions = new Action[]
        {
            () => Assert.Equal(createdOutgoingMessageId.Value, result!.Id),
            () => Assert.NotNull(result!.RecordId),
            () => Assert.Equal(message.ProcessId, result!.ProcessId),
            () => Assert.Equal(DocumentType.NotifyAggregatedMeasureData.Name, result!.DocumentType),
            () => Assert.Equal(message.ReceiverNumber.Value, result!.ReceiverNumber),
            () => Assert.Equal(message.ReceiverRole.Code, result!.ReceiverRole),
            () => Assert.Equal(message.ReceiverNumber.Value, result!.DocumentReceiverNumber),
            () => Assert.Equal(message.ReceiverRole.Code, result!.DocumentReceiverRole),
            () => Assert.Equal(message.SenderId.Value, result!.SenderId),
            () => Assert.Equal(message.SenderRole.Code, result!.SenderRole),
            () => Assert.Equal(message.BusinessReason, result!.BusinessReason),
            () => Assert.Equal(message.Series.GridAreaCode, result!.GridAreaCode),
            () => Assert.Equal(ProcessType.ReceiveEnergyResults.Name, result!.MessageCreatedFromProcess),
            () => Assert.Equal(expectedFileStorageReference, result!.FileStorageReference),
            () => Assert.Equal("OutgoingMessage", result!.Discriminator),
            () => Assert.Equal(message.RelatedToMessageId?.Value, result!.RelatedToMessageId),
            () => Assert.NotNull(result!.AssignedBundleId),
        };

        Assert.Multiple(propertyAssertions);

        // Confirm that all database columns are asserted
        var databaseColumnsCount = ((IDictionary<string, object>)result!).Count;
        var propertiesAssertedCount = propertyAssertions.Length;
        propertiesAssertedCount.Should().Be(databaseColumnsCount, "asserted properties count should be equal to OutgoingMessage database columns count");
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = _energyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                message.ReceiverNumber,
                MessageCategory.Aggregations,
                message.ReceiverRole,
                DocumentFormat.Xml),
            CancellationToken.None);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_peek_oldest_bundle()
    {
        var message = _energyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        _systemDateTimeProvider.SetNow(_systemDateTimeProvider.Now().PlusSeconds(1));
        var message2 = _energyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message2);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(message.ReceiverNumber, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Ebix), CancellationToken.None);
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT top 1 id FROM [dbo].[Bundles] order by created";
        var id = await
            connection
                .QuerySingleOrDefaultAsync<Guid>(sql);

        Assert.Equal(result.MessageId, id);
    }

    [Fact]
    public async Task Can_dequeue_bundle()
    {
        var message = _energyResultMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        var peekRequestDto = new PeekRequestDto(
            message.ReceiverNumber,
            MessageCategory.Aggregations,
            message.ReceiverRole,
            DocumentFormat.Xml);
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(peekRequestDto, CancellationToken.None);
        var dequeueCommand = new DequeueRequestDto(
            peekResult.MessageId!.Value.ToString(),
            message.ReceiverRole,
            message.ReceiverNumber);

        var result = await _outgoingMessagesClient.DequeueAndCommitAsync(dequeueCommand, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Outgoing_messages_for_same_actor_is_added_to_existing_bundle()
    {
        // Arrange
        var actorMessageQueueId = Guid.NewGuid();
        var existingBundleId = Guid.NewGuid();
        var message = _energyResultMessageDtoBuilder
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
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();
        var outgoingMessage = OutgoingMessage.CreateMessage(
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
        var message = _energyResultMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);
        var fileStorageReference = await GetOutgoingMessageFileStorageReferenceFromDatabase(createdId);
        var uploadDuplicateFile = async () => await _fileStorageClient.UploadAsync(new FileStorageReference(OutgoingMessage.FileStorageCategory, fileStorageReference!), new MemoryStream(new byte[] { 0x20 }));

        // Assert
        (await uploadDuplicateFile.Should().ThrowAsync<RequestFailedException>())
            .And.ErrorCode.Should().Be("BlobAlreadyExists");
    }

    [Fact]
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

        Assert.Equal(existingBundleId, bundleIdForMessage1);
        Assert.NotEqual(existingBundleId, bundleIdForMessage2);
        Assert.NotEqual(existingBundleId, bundleIdForMessage3);

        Assert.NotEqual(bundleIdForMessage2, bundleIdForMessage3);
    }

    [Fact]
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
    /// </summary>
    [Fact]
    public async Task Given_EnqueuingNotifyAggregatedMeasureData_When_ReceiverActorRoleIsMDR_Then_MessageShouldBeEnqueuedAsDDM()
    {
        // Arrange
        var message = _energyResultMessageDtoBuilder
            .WithReceiverRole(ActorRole.MeteredDataResponsible)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var fromDb = await GetOutgoingMessageWithActorMessageQueueFromDatabase(createdId);

        fromDb.ActorMessageQueueNumber.Should().Be(message.ReceiverNumber.Value);
        fromDb.ActorMessageQueueRole.Should().Be(ActorRole.GridOperator.Code);
        fromDb.OutgoingMessageReceiverRole.Should().Be(ActorRole.MeteredDataResponsible.Code);
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private async Task<(string ActorMessageQueueNumber, string ActorMessageQueueRole, string OutgoingMessageReceiverRole)> GetOutgoingMessageWithActorMessageQueueFromDatabase(OutgoingMessageId createdId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var result = await connection.QuerySingleAsync(
            @"SELECT tQueue.ActorNumber, tQueue.ActorRole, tOutgoing.ReceiverRole FROM [dbo].[OutgoingMessages] AS tOutgoing
                    INNER JOIN [dbo].[Bundles] as tBundle ON tOutgoing.AssignedBundleId = tBundle.Id
                    INNER JOIN [dbo].ActorMessageQueues as tQueue on tBundle.ActorMessageQueueId = tQueue.Id",
            new
                {
                    Id = createdId.Value.ToString(),
                });

        return (ActorMessageQueueNumber: result.ActorNumber, ActorMessageQueueRole: result.ActorRole, OutgoingMessageReceiverRole: result.ReceiverRole);
    }

    private async Task<string?> GetOutgoingMessageFileStorageReferenceFromDatabase(OutgoingMessageId id)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[OutgoingMessages] WHERE Id = '{id.Value}'");

        return fileStorageReference;
    }

    private async Task<Guid> GetOutgoingMessageBundleIdFromDatabase(OutgoingMessageId id)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var assignedBundleId = await connection.ExecuteScalarAsync<Guid>($"SELECT AssignedBundleId FROM [dbo].[OutgoingMessages] WHERE Id = '{id.Value}'");

        return assignedBundleId;
    }

    private async Task<OutgoingMessageId> EnqueueAndCommitAsync(EnergyResultMessageDto message)
    {
        return await _outgoingMessagesClient.EnqueueAndCommitAsync(message, CancellationToken.None);
    }

    private async Task<OutgoingMessageId> EnqueueAndCommitAsync(RejectedEnergyResultMessageDto message)
    {
        var outgoingMessageId = await _outgoingMessagesClient.EnqueueAsync(message, CancellationToken.None);
        var actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        await actorMessageQueueContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        return outgoingMessageId;
    }

    private async Task CreateActorMessageQueueInDatabase(Guid id, ActorNumber actorNumber, ActorRole actorRole)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[ActorMessageQueues] (Id, ActorNumber, ActorRole)
                    VALUES (@Id, @ActorNumber, @ActorRole)",
            new
            {
                Id = id,
                ActorNumber = actorNumber.Value,
                ActorRole = actorRole.Code,
            });
    }

    private async Task CreateBundleInDatabase(Guid id, Guid actorMessageQueueId, DocumentType documentType, string businessReason, int? maxMessageCount = null, MessageId? relatedToMessageId = null)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[Bundles] (Id, ActorMessageQueueId, DocumentTypeInBundle, IsDequeued, IsClosed, MessageCount, MaxMessageCount, BusinessReason, Created, RelatedToMessageId)
                    VALUES (@Id, @ActorMessageQueueId, @DocumentTypeInBundle, @IsDequeued, @IsClosed, @MessageCount, @MaxMessageCount, @BusinessReason, @Created, @RelatedToMessageId)",
            new
            {
                Id = id,
                ActorMessageQueueId = actorMessageQueueId,
                DocumentTypeInBundle = documentType.Name,
                IsDequeued = false,
                IsClosed = false,
                MessageCount = 0,
                MaxMessageCount = maxMessageCount ?? 1,
                BusinessReason = businessReason,
                Created = new DateTime(2022, 2, 2),
                RelatedToMessageId = relatedToMessageId?.Value,
            });
    }
}
