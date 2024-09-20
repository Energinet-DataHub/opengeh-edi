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
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesIntegrationTestCollectionFixture))]
public class WhenArchivedMessageIsCreatedTests : IClassFixture<ArchivedMessagesFixture>
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    public WhenArchivedMessageIsCreatedTests(ArchivedMessagesFixture fixture)
    {
        _fixture = fixture;
        _sut = fixture.ArchivedMessagesClient;
        _fixture.CleanupDatabase();
        _fixture.CleanupFileStorage();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_Creating_Then_MessageIsStoredInDatabaseAndBlob()
    {
        // Arrange
        var archivedMessage = CreateArchivedMessage(ArchivedMessageType.IncomingMessage);
        var messageCreatedAt = archivedMessage.CreatedAt.ToDateTimeUtc();

        // Since the message is an incoming message, we need the "senderNumber" in the path
        var expectedBlobPath = $"{archivedMessage.SenderNumber.Value}/"
                               + $"{messageCreatedAt.Year:0000}/"
                               + $"{messageCreatedAt.Month:00}/"
                               + $"{messageCreatedAt.Day:00}/"
                               + $"{archivedMessage.Id.Value:N}"; // remove dashes from guid

        var expectedBlocReference = new FileStorageReference(
            category: FileStorageCategory.ArchivedMessage(),
            path: expectedBlobPath);

        // Act
        await _sut.CreateAsync(archivedMessage, CancellationToken.None);

        // Assert
        var dbResult = await GetAllMessagesInDatabase();

        var message = dbResult.Single();
        using var assertionScope = new AssertionScope();

        message.SenderNumber.Should().Be(archivedMessage.SenderNumber.Value);
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.DocumentType.Should().Be(archivedMessage.DocumentType);
        message.ReceiverNumber.Should().Be(archivedMessage.ReceiverNumber.Value);
        message.BusinessReason.Should().Be(archivedMessage.BusinessReason);
        message.FileStorageReference.Should().Be(expectedBlocReference.Path);
        message.RelatedToMessageId.Should().BeNull();
        message.EventIds.Should().BeNull();

        var blobResult = await GetMessagesFromBlob(expectedBlocReference);
        blobResult.Should().NotBeNull();
    }

    private static ArchivedMessage CreateArchivedMessage(
        ArchivedMessageType? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? senderNumber = null,
        string? receiverNumber = null,
        Instant? timestamp = null)
    {
        var documentStream = new MemoryStream();

        if (!string.IsNullOrEmpty(documentContent))
        {
            var streamWriter = new StreamWriter(documentStream);
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        return new ArchivedMessage(
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            Array.Empty<EventId>(),
            DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            ActorRole.MeteredDataAdministrator,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            ActorRole.DanishEnergyAgency,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageType.IncomingMessage,
            new ArchivedMessageStream(documentStream),
            null);
    }

    private async Task<IReadOnlyCollection<ArchivedMessageFromDb>> GetAllMessagesInDatabase()
    {
        var connectionFactory = _fixture.ServiceProvider.GetService<IDatabaseConnectionFactory>()!;
        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var archivedMessages =
            await connection.QueryAsync<ArchivedMessageFromDb>(
                    "SELECT "
                    + "Id,"
                    + " MessageId,"
                    + " DocumentType,"
                    + " SenderNumber,"
                    + " ReceiverNumber,"
                    + " CreatedAt,"
                    + " BusinessReason,"
                    + " FileStorageReference,"
                    + " RelatedToMessageId,"
                    + " EventIds"
                    + " FROM dbo.[ArchivedMessages]")
                .ConfigureAwait(false);

        return archivedMessages.ToList().AsReadOnly();
    }

    private async Task<ArchivedMessageStream> GetMessagesFromBlob(FileStorageReference reference)
    {
        var blobClient = _fixture.ServiceProvider.GetService<IFileStorageClient>()!;

        var fileStorageFile = await blobClient.DownloadAsync(reference).ConfigureAwait(false);
        return new ArchivedMessageStream(fileStorageFile);
    }
}
