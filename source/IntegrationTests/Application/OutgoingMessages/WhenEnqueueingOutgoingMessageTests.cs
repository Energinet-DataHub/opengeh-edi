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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
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
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;
    private readonly SystemDateTimeProviderStub _systemDateTimeProvider;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ActorMessageQueueContext _context;
    private readonly IFileStorageClient _fileStorageClient;

    public WhenEnqueueingOutgoingMessageTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        _fileStorageClient = GetService<IFileStorageClient>();
        _systemDateTimeProvider = (SystemDateTimeProviderStub)GetService<ISystemDateTimeProvider>();
        _context = GetService<ActorMessageQueueContext>();
    }

    [Fact]
    public async Task Outgoing_message_is_added_to_database_with_correct_values()
    {
        // Arrange
        var message = _outgoingMessageDtoBuilder
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
            () => Assert.Equal(createdOutgoingMessageId.Value, result.Id),
            () => Assert.NotNull(result.RecordId),
            () => Assert.Equal(message.ProcessId, result.ProcessId),
            () => Assert.Equal(DocumentType.NotifyAggregatedMeasureData.Name, result.DocumentType),
            () => Assert.Equal(message.ReceiverId.Value, result.ReceiverId),
            () => Assert.Equal(message.ReceiverRole.Name, result.ReceiverRole),
            () => Assert.Equal(message.SenderId.Value, result.SenderId),
            () => Assert.Equal(message.SenderRole.Name, result.SenderRole),
            () => Assert.Equal(message.BusinessReason, result.BusinessReason),
            () => Assert.Equal(expectedFileStorageReference, result.FileStorageReference),
            () => Assert.Equal("OutgoingMessage", result.Discriminator),
            () => Assert.False(result.IsPublished),
            () => Assert.NotNull(result.AssignedBundleId),
        };

        Assert.Multiple(propertyAssertions);

        // Confirm that all database columns are asserted
        var databaseColumnsCount = ((IDictionary<string, object>)result).Count;
        var propertiesAssertedCount = propertyAssertions.Length;
        propertiesAssertedCount.Should().Be(databaseColumnsCount, "asserted properties count should be equal to OutgoingMessage database columns count");
    }

    [Fact]
    public async Task Can_peek_message()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequestDto(
                message.ReceiverId,
                MessageCategory.Aggregations,
                message.ReceiverRole,
                DocumentFormat.Xml),
            CancellationToken.None);

        Assert.NotNull(result.MessageId);
    }

    [Fact]
    public async Task Can_peek_oldest_bundle()
    {
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        _systemDateTimeProvider.SetNow(_systemDateTimeProvider.Now().PlusSeconds(1));
        var message2 = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message2);

        var result = await _outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(message.ReceiverId, message.DocumentType.Category, message.ReceiverRole, DocumentFormat.Ebix), CancellationToken.None);
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
        var message = _outgoingMessageDtoBuilder.Build();
        await EnqueueAndCommitAsync(message);
        var peekRequestDto = new PeekRequestDto(
            message.ReceiverId,
            MessageCategory.Aggregations,
            message.ReceiverRole,
            DocumentFormat.Xml);
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(peekRequestDto, CancellationToken.None);
        var dequeueCommand = new DequeueRequestDto(
            peekResult.MessageId!.Value.ToString(),
            message.ReceiverRole,
            message.ReceiverId);

        var result = await _outgoingMessagesClient.DequeueAndCommitAsync(dequeueCommand, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Outgoing_message_record_is_added_to_file_storage_with_correct_content()
    {
        // Arrange
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);

        // Assert
        var fileStorageReference = await GetOutgoingMessageFileStorageReferenceFromDatabase(createdId);

        var fileContent = await GetFileFromFileStorage(fileStorageReference);

        fileContent.HasValue.Should().BeTrue();

        var fileContentAsString = await GetStreamContentAsString(fileContent.Value.Content);
        fileContentAsString.Should().Be(message.MessageRecord);
    }

    [Fact]
    public async Task Uploading_duplicate_outgoing_message_to_file_storage_throws_exception()
    {
        // Arrange
        var message = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .Build();

        // Act
        var createdId = await EnqueueAndCommitAsync(message);
        var fileStorageReference = await GetOutgoingMessageFileStorageReferenceFromDatabase(createdId);
        var uploadDuplicateFile = async () => await _fileStorageClient.UploadAsync("outgoing", new FileStorageReference(fileStorageReference), new MemoryStream(new byte[] { 0x20 }));

        // Assert
        (await uploadDuplicateFile.Should().ThrowAsync<RequestFailedException>())
            .And.ErrorCode.Should().Be("BlobAlreadyExists");
    }

    protected override void Dispose(bool disposing)
    {
        _context.Dispose();
        base.Dispose(disposing);
    }

    private static async Task<string> GetStreamContentAsString(Stream stream)
    {
        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var stringContent = await streamReader.ReadToEndAsync();

        return stringContent;
    }

    private static async Task<Response<BlobDownloadInfo>> GetFileFromFileStorage(string fileStorageReference)
    {
        var azuriteBlobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
        var blobServiceClient = new BlobServiceClient(azuriteBlobConnectionString);

        var container = blobServiceClient.GetBlobContainerClient("outgoing");
        var blob = container.GetBlobClient(fileStorageReference);

        var blobContent = await blob.DownloadAsync();
        return blobContent;
    }

    private async Task<string> GetOutgoingMessageFileStorageReferenceFromDatabase(OutgoingMessageId id)
    {
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[OutgoingMessages] WHERE Id = '{id.Value}'");

        return fileStorageReference;
    }

    private async Task<OutgoingMessageId> EnqueueAndCommitAsync(OutgoingMessageDto message)
    {
        var outgoingMessageId = await _outgoingMessagesClient.EnqueueAsync(message);
        await _context.SaveChangesAsync();

        return outgoingMessageId;
    }
}
