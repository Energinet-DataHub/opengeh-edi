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

using Energinet.DataHub.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces.FileStorage;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.ArchivedMessages;

public class WhenArchivedMessageIsCreatedTests : TestBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;

    public WhenArchivedMessageIsCreatedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _archivedMessagesClient = GetService<IArchivedMessagesClient>();
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_by_id()
    {
        var correctArchivedMessage = CreateArchivedMessage();
        await ArchiveMessage(CreateArchivedMessage());
        await ArchiveMessage(correctArchivedMessage);
        await ArchiveMessage(CreateArchivedMessage());

        var result = await _archivedMessagesClient.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_with_correct_content()
    {
        // Arrange
        var correctDocumentContent = "correct document content";
        var correctArchivedMessage = CreateArchivedMessage(documentContent: correctDocumentContent);
        await ArchiveMessage(CreateArchivedMessage(documentContent: "incorrect document content"));
        await ArchiveMessage(correctArchivedMessage);
        await ArchiveMessage(CreateArchivedMessage(documentContent: "incorrect document content"));

        // Act
        var result = await _archivedMessagesClient.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var streamReader = new StreamReader(result!.Stream);
        var actualDocumentContent = await streamReader.ReadToEndAsync();
        actualDocumentContent.Should().Be(correctDocumentContent);
    }

    [Theory]
    [InlineData(ArchivedMessageTypeDto.IncomingMessage)]
    [InlineData(ArchivedMessageTypeDto.OutgoingMessage)]
    public async Task Archived_document_is_saved_at_correct_path(ArchivedMessageTypeDto archivedMessageType)
    {
        var messageId = MessageId.New();
        var senderNumber = "1122334455667788";
        var receiverNumber = "8877665544332211";
        int year = 2024,
            month = 01,
            date = 25;

        var archivedMessage = CreateArchivedMessage(
            archivedMessageType: archivedMessageType,
            messageId: messageId.Value,
            senderNumber: senderNumber,
            receiverNumber: receiverNumber,
            timestamp: Instant.FromUtc(year, month, date, 0, 0));

        var mappedArchiveMessage = ArchivedMessageMapper.Map(archivedMessage);

        var expectedActorNumber = archivedMessageType == ArchivedMessageTypeDto.IncomingMessage ? senderNumber : receiverNumber;
        var expectedFileStorageReference = $"{expectedActorNumber}/{year:000}/{month:00}/{date:00}/{archivedMessage.Id.Value:N}";

        await ArchiveMessage(archivedMessage);

        var actualFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId.Value);

        using var assertionScope = new AssertionScope();
        mappedArchiveMessage.FileStorageReference.Category.Value.Should().Be("archived");
        actualFileStorageReference.Should().Be(expectedFileStorageReference);
    }

    [Fact]
    public async Task Id_has_to_be_unique_in_database()
    {
        var archivedMessage = CreateArchivedMessage();
        var serviceProviderWithoutFileStorage = BuildServiceProviderWithoutFileStorage();
        var clientWithoutFileStorage = serviceProviderWithoutFileStorage.GetRequiredService<IArchivedMessagesClient>();

        await clientWithoutFileStorage.CreateAsync(archivedMessage, CancellationToken.None);
        var createDuplicateArchivedMessage = new Func<Task>(() => clientWithoutFileStorage.CreateAsync(archivedMessage, CancellationToken.None));

        await createDuplicateArchivedMessage.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Id_has_to_be_unique_in_file_storage()
    {
        var archivedMessage = CreateArchivedMessage();

        await ArchiveMessage(archivedMessage);
        var createDuplicateArchivedMessage = () => ArchiveMessage(archivedMessage);

        await createDuplicateArchivedMessage.Should().ThrowAsync<Azure.RequestFailedException>();
    }

    [Fact]
    public async Task Adding_archived_message_with_existing_message_id_creates_new_archived_message()
    {
        var messageId = "MessageId";
        var archivedMessage1 = CreateArchivedMessage(messageId: messageId);
        var archivedMessage2 = CreateArchivedMessage(messageId: messageId);

        await ArchiveMessage(archivedMessage1);

        try
        {
            await ArchiveMessage(archivedMessage2);
        }
#pragma warning disable CA1031  // We want to catch all exceptions
        catch
#pragma warning restore CA1031
        {
            Assert.Fail("We should be able to save multiple messages with the same message id");
        }

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQueryDto(new SortedCursorBasedPaginationDto()), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(messageId, result.Messages[0].MessageId);
        Assert.Equal(messageId, result.Messages[1].MessageId);
    }

    private static ArchivedMessageDto CreateArchivedMessage(
        ArchivedMessageTypeDto? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? senderNumber = null,
        string? receiverNumber = null,
        Instant? timestamp = null)
    {
#pragma warning disable CA2000 // Don't dispose stream
        var documentStream = new MarketDocumentWriterMemoryStream();
#pragma warning restore CA2000

        if (!string.IsNullOrEmpty(documentContent))
        {
            // Don't dispose streamWriter, it disposes the documentStream as well
#pragma warning disable CA2000
            var streamWriter = new StreamWriter(documentStream);
#pragma warning restore CA2000
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        return new ArchivedMessageDto(
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            new[] { EventId.From(Guid.NewGuid()) },
            DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            ActorRole.EnergySupplier,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            ActorRole.EnergySupplier,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageTypeDto.OutgoingMessage,
            new MarketDocumentStream(documentStream));
    }

    private async Task ArchiveMessage(ArchivedMessageDto archivedMessage)
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
