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

using Dapper;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class WhenArchivedMessageIsCreatedTests : IAsyncLifetime
{
    private static readonly Guid _actorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ArchivedMessagesFixture _fixture;
    private readonly ActorIdentity _authenticatedActor = new(
        ActorNumber.Create("1234512345811"),
        Restriction.None,
        ActorRole.MeteredDataAdministrator,
        _actorId);

    public WhenArchivedMessageIsCreatedTests(ArchivedMessagesFixture archivedMessagesFixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = archivedMessagesFixture;
        var services = _fixture.BuildService(testOutputHelper);
        services.GetRequiredService<AuthenticatedActor>().SetAuthenticatedActor(_authenticatedActor);
        _archivedMessagesClient = services.GetRequiredService<IArchivedMessagesClient>();
    }

    public Task InitializeAsync()
    {
        _fixture.CleanupDatabase();
        _fixture.CleanupFileStorage();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_by_id()
    {
        var correctArchivedMessage = await CreateArchivedMessageAsync();

        var result = await _archivedMessagesClient.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Archived_document_can_be_retrieved_with_correct_content()
    {
        // Arrange
        var correctDocumentContent = "correct document content";
        var correctArchivedMessage = await CreateArchivedMessageAsync(documentContent: correctDocumentContent);
        await CreateArchivedMessageAsync(documentContent: "incorrect document content");

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

        var archivedMessage = await CreateArchivedMessageAsync(
            archivedMessageType: archivedMessageType,
            messageId: messageId.Value,
            senderNumber: senderNumber,
            receiverNumber: receiverNumber,
            timestamp: Instant.FromUtc(year, month, date, 0, 0));

        var mappedArchiveMessage = ArchivedMessageMapper.Map(archivedMessage);

        var expectedActorNumber = archivedMessageType == ArchivedMessageTypeDto.IncomingMessage ? senderNumber : receiverNumber;
        var expectedFileStorageReference = $"{expectedActorNumber}/{year:000}/{month:00}/{date:00}/{archivedMessage.Id.Value:N}";

        var actualFileStorageReference = await GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId.Value);

        using var assertionScope = new AssertionScope();
        mappedArchiveMessage.FileStorageReference.Category.Value.Should().Be("archived");
        actualFileStorageReference.Should().Be(expectedFileStorageReference);
    }

    [Fact]
    public async Task Id_has_to_be_unique_in_database()
    {
        var serviceProviderWithoutFileStorage = BuildServiceProviderWithoutFileStorage();
        var clientWithoutFileStorage = serviceProviderWithoutFileStorage.GetRequiredService<IArchivedMessagesClient>();
        var archivedMessageDto = new ArchivedMessageDto(
            Guid.NewGuid().ToString(),
            new[] { EventId.From(Guid.NewGuid()) },
            DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create("1234512345123"),
            ActorRole.EnergySupplier,
            ActorNumber.Create("1234512345128"),
            ActorRole.EnergySupplier,
            Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing.Name,
            ArchivedMessageTypeDto.OutgoingMessage,
            new ArchivedMessageStreamDto(new MemoryStream()));
        await clientWithoutFileStorage.CreateAsync(archivedMessageDto, CancellationToken.None);
        var createDuplicateArchivedMessage = new Func<Task>(() => clientWithoutFileStorage.CreateAsync(archivedMessageDto, CancellationToken.None));

        await createDuplicateArchivedMessage.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Adding_archived_message_with_existing_message_id_creates_new_archived_message()
    {
        var messageId = "MessageId";
        await CreateArchivedMessageAsync(messageId: messageId);
        await CreateArchivedMessageAsync(messageId: messageId);

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQueryDto(new SortedCursorBasedPaginationDto()), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(messageId, result.Messages[0].MessageId);
        Assert.Equal(messageId, result.Messages[1].MessageId);
    }

    private async Task<ArchivedMessageDto> CreateArchivedMessageAsync(
        ArchivedMessageTypeDto? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? senderNumber = null,
        string? receiverNumber = null,
        Instant? timestamp = null)
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

        return await _fixture.CreateArchivedMessageAsync(
            archivedMessageType ?? ArchivedMessageTypeDto.OutgoingMessage,
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            documentContent,
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123").Value,
            ActorRole.EnergySupplier,
            ActorNumber.Create(receiverNumber ?? "1234512345128").Value,
            ActorRole.EnergySupplier,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0));
    }

    private ServiceProvider BuildServiceProviderWithoutFileStorage()
    {
        var serviceCollectionWithoutFileStorage = _fixture.GetServiceCollectionClone();
        serviceCollectionWithoutFileStorage.RemoveAll<IFileStorageClient>();
        serviceCollectionWithoutFileStorage.AddScoped<IFileStorageClient, FileStorageClientStub>();

        var dependenciesWithoutFileStorage = serviceCollectionWithoutFileStorage.BuildServiceProvider();
        return dependenciesWithoutFileStorage;
    }

    private async Task<string?> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(string messageId)
    {
        using var connection =
            await _fixture.Services.GetRequiredService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);

        var fileStorageReference = await connection.ExecuteScalarAsync<string>(
            $"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return fileStorageReference;
    }
}
