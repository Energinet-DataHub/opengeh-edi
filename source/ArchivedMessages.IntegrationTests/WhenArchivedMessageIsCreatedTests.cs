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

using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
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
    public async Task Given_ArchivedMessage_When_Creating_Then_MessageIsStored()
    {
        // Arrange
        var correctArchivedMessage = CreateArchivedMessage();

        // Act
        await _sut.CreateAsync(correctArchivedMessage, CancellationToken.None);

        // Assert
        var dbResult = await _sut.SearchAsync(new GetMessagesQuery(), CancellationToken.None);
        var blobResult = await _sut.GetAsync(correctArchivedMessage.Id, CancellationToken.None);

        dbResult.Messages.Should().HaveCount(1);
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
            senderNumber ?? "1234512345123",
            receiverNumber ?? "1234512345128",
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageType.IncomingMessage,
            new ArchivedMessageStream(documentStream));
    }
}
