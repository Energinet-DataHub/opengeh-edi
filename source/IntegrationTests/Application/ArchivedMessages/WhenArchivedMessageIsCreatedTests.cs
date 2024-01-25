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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.ArchivedMessages;

public class WhenArchivedMessageIsCreatedTests : TestBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;

    public WhenArchivedMessageIsCreatedTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
        _archivedMessagesClient = GetService<IArchivedMessagesClient>();
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_by_id()
    {
        var id = Guid.NewGuid().ToString();
        await ArchiveMessage(CreateArchivedMessage());
        await ArchiveMessage(CreateArchivedMessage(id));
        await ArchiveMessage(CreateArchivedMessage());

        var result = await _archivedMessagesClient.GetAsync(id, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_with_correct_content()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var correctDocumentContent = "correct document content";
        await ArchiveMessage(CreateArchivedMessage(documentContent: "incorrect document content"));
        await ArchiveMessage(CreateArchivedMessage(id: id, documentContent: correctDocumentContent));
        await ArchiveMessage(CreateArchivedMessage(documentContent: "incorrect document content"));

        // Act
        await using var result = await _archivedMessagesClient.GetAsync(id, CancellationToken.None);

        // Assert
        using var assertionScope = new AssertionScope();
        result.Should().NotBeNull();
        using var streamReader = new StreamReader(result!);
        var actualDocumentContent = await streamReader.ReadToEndAsync();
        actualDocumentContent.Should().Be(correctDocumentContent);
    }

    [Fact]
    public async Task Archived_document_is_saved_at_correct_path()
    {
        var id = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var receiverNumber = "1122334455667788";
        var timestamp = Instant.FromUtc(2024, 01, 25, 0, 0);

        var timestampInUtc = timestamp.ToDateTimeUtc();
        var expectedFileStorageReference = $"{receiverNumber}/{timestampInUtc.Year:000}/{timestampInUtc.Month:00}/{timestampInUtc.Day:00}/{id}";

        await ArchiveMessage(CreateArchivedMessage(
            id: id.ToString(),
            messageId: messageId.ToString(),
            receiverNumber: receiverNumber,
            timestamp: timestamp));

        var fileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId);

        fileStorageReference.Should().Be(expectedFileStorageReference);
    }

    [Fact]
    public async Task Id_has_to_be_unique_in_database()
    {
        var id = Guid.NewGuid().ToString();
        var serviceProviderWithoutFileStorage = BuildServiceProviderWithoutFileStorage();
        var clientWithoutFileStorage = serviceProviderWithoutFileStorage.GetRequiredService<IArchivedMessagesClient>();

        await clientWithoutFileStorage.CreateAsync(CreateArchivedMessage(id), CancellationToken.None);
        var createDuplicateArchivedMessage = new Func<Task>(() => clientWithoutFileStorage.CreateAsync(CreateArchivedMessage(id), CancellationToken.None));

        await createDuplicateArchivedMessage.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Id_has_to_be_unique_in_file_storage()
    {
        var id = Guid.NewGuid().ToString();

        await ArchiveMessage(CreateArchivedMessage(id));
        var createDuplicateArchivedMessage = () => ArchiveMessage(CreateArchivedMessage(id));

        await createDuplicateArchivedMessage.Should().ThrowAsync<Azure.RequestFailedException>();
    }

    [Fact]
    public async Task Adding_archived_message_with_existing_message_id_creates_new_archived_message()
    {
        var id1 = Guid.NewGuid().ToString();
        var id2 = Guid.NewGuid().ToString();
        var messageId = "MessageId";
        await ArchiveMessage(CreateArchivedMessage(id1, messageId));

        try
        {
            await ArchiveMessage(CreateArchivedMessage(id2, messageId));
        }
#pragma warning disable CA1031  // We want to catch all exceptions
        catch
#pragma warning restore CA1031
        {
            Assert.Fail("We should be able to save multiple messages with the same message id");
        }

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(messageId, result.Messages[0].MessageId);
        Assert.Equal(messageId, result.Messages[1].MessageId);
    }

    private static ArchivedMessage CreateArchivedMessage(string? id = null, string? messageId = null, string? documentContent = null, string? receiverNumber = null, Instant? timestamp = null)
    {
        var documentStream = new MemoryStream();

        if (!string.IsNullOrEmpty(documentContent))
        {
            // Don't dispose streamWriter, it disposes the documentStream as well
#pragma warning disable CA2000
            var streamWriter = new StreamWriter(documentStream);
#pragma warning restore CA2000
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        return new ArchivedMessage(
            string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id,
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create("1234512345123"),
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing.Name,
            documentStream);
    }

    private async Task ArchiveMessage(ArchivedMessage archivedMessage)
    {
        await _archivedMessagesClient.CreateAsync(archivedMessage, CancellationToken.None);
    }

    private ServiceProvider BuildServiceProviderWithoutFileStorage()
    {
        var serviceCollectionWithoutFileStorage = GetServiceCollectionClone();
        serviceCollectionWithoutFileStorage.RemoveAll<IFileStorageClient>();
        serviceCollectionWithoutFileStorage.AddScoped<IFileStorageClient, FileStorageClientStub>();

        var dependenciesWithoutFileStorage = serviceCollectionWithoutFileStorage.BuildServiceProvider();
        return dependenciesWithoutFileStorage;
    }
}
