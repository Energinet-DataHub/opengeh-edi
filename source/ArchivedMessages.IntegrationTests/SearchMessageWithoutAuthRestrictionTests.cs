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
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class SearchMessageWithoutAuthRestrictionTests
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    public SearchMessageWithoutAuthRestrictionTests(ArchivedMessagesFixture fixture)
    {
        _fixture = fixture;
        _sut = fixture.ArchivedMessagesClient;
        _fixture.CleanupDatabase();
        _fixture.CleanupFileStorage();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_SearchingWithoutCriteria_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var archivedMessage = await CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        var message = result.Messages.Should().ContainSingle().Subject;
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.SenderNumber.Should().Be(archivedMessage.SenderNumber.Value);
        message.ReceiverNumber.Should().Be(archivedMessage.ReceiverNumber.Value);
        message.DocumentType.Should().Be(archivedMessage.DocumentType);
        message.BusinessReason.Should().Be(archivedMessage.BusinessReason);
        message.CreatedAt.Should().Be(archivedMessage.CreatedAt);
        message.Id.Should().Be(archivedMessage.Id.Value);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByDate_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedCreatedAt = CreatedAt("2023-05-01T22:00:00Z");
        await CreateArchivedMessageAsync(timestamp: expectedCreatedAt.PlusDays(-1));
        await CreateArchivedMessageAsync(timestamp: expectedCreatedAt);
        await CreateArchivedMessageAsync(timestamp: expectedCreatedAt.PlusDays(1));

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                new MessageCreationPeriod(
                    expectedCreatedAt.PlusHours(-2),
                    expectedCreatedAt.PlusHours(2))),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().ContainSingle().Subject.CreatedAt.Should().Be(expectedCreatedAt);
    }

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private async Task<ArchivedMessage> CreateArchivedMessageAsync(
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

        var archivedMessage = new ArchivedMessage(
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

        await _sut.CreateAsync(archivedMessage, CancellationToken.None);

        return archivedMessage;
    }
}
