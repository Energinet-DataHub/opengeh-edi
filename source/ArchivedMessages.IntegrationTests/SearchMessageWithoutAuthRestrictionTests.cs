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

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByDateAndMessageId_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        var expectedCreatedAt = CreatedAt("2023-05-01T22:00:00Z");
        await CreateArchivedMessageAsync(timestamp: expectedCreatedAt, messageId: expectedMessageId);
        await CreateArchivedMessageAsync(timestamp: expectedCreatedAt.PlusDays(1));

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                CreationPeriod: new MessageCreationPeriod(
                    expectedCreatedAt.PlusHours(-2),
                    expectedCreatedAt.PlusHours(2)),
                MessageId: expectedMessageId),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        var message = result.Messages.Should().ContainSingle().Subject;
        message.CreatedAt.Should().Be(expectedCreatedAt);
        message.MessageId.Should().Be(expectedMessageId);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingBySenderNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedSenderNumber = "9999999999999";
        await CreateArchivedMessageAsync(senderNumber: expectedSenderNumber);
        await CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                SenderNumber: expectedSenderNumber),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle().Subject
            .SenderNumber.Should().Be(expectedSenderNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByReceiverNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedReceiverNumber = "9999999999999";
        await CreateArchivedMessageAsync(receiverNumber: expectedReceiverNumber);
        await CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                ReceiverNumber: expectedReceiverNumber),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle().Subject
            .ReceiverNumber.Should().Be(expectedReceiverNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByMessageId_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        await CreateArchivedMessageAsync(messageId: expectedMessageId);
        await CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: expectedMessageId),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle().Subject
            .MessageId.Should().Be(expectedMessageId);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByDocumentType_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedDocumentType = DocumentType.NotifyAggregatedMeasureData.Name;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData.Name;
        await CreateArchivedMessageAsync(documentType: expectedDocumentType);
        await CreateArchivedMessageAsync(documentType: unexpectedDocumentType);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                DocumentTypes: new List<string>
                {
                    expectedDocumentType,
                }),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle().Subject
            .DocumentType.Should().Be(expectedDocumentType);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByDocumentTypes_Then_ReturnsExpectedMessages()
    {
        // Arrange
        var expectedDocumentType1 = DocumentType.NotifyAggregatedMeasureData.Name;
        var expectedDocumentType2 = DocumentType.NotifyWholesaleServices.Name;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData.Name;
        await CreateArchivedMessageAsync(documentType: expectedDocumentType1);
        await CreateArchivedMessageAsync(documentType: expectedDocumentType2);
        await CreateArchivedMessageAsync(documentType: unexpectedDocumentType);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                DocumentTypes: new List<string>
                {
                    expectedDocumentType1,
                    expectedDocumentType2,
                }),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().HaveCount(2);
        result.Messages.Select(message => message.DocumentType)
            .Should()
            .BeEquivalentTo(new List<string> { expectedDocumentType1, expectedDocumentType2, });
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByBusinessReason_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedBusinessReason = BusinessReason.MoveIn.Name;
        var unexpectedBusinessReason = BusinessReason.BalanceFixing.Name;
        await CreateArchivedMessageAsync(businessReasons: expectedBusinessReason);
        await CreateArchivedMessageAsync(businessReasons: unexpectedBusinessReason);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                BusinessReasons: new List<string>
                {
                    expectedBusinessReason,
                }),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle().Subject
            .BusinessReason.Should().Be(expectedBusinessReason);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByBusinessReasons_Then_ReturnsExpectedMessages()
    {
        // Arrange
        var expectedBusinessReason1 = BusinessReason.MoveIn.Name;
        var expectedBusinessReason2 = BusinessReason.Correction.Name;
        var unexpectedBusinessReason = BusinessReason.BalanceFixing.Name;
        await CreateArchivedMessageAsync(businessReasons: expectedBusinessReason1);
        await CreateArchivedMessageAsync(businessReasons: expectedBusinessReason2);
        await CreateArchivedMessageAsync(businessReasons: unexpectedBusinessReason);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                BusinessReasons: new List<string>
                {
                    expectedBusinessReason1,
                    expectedBusinessReason2,
                }),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().HaveCount(2);
        result.Messages.Select(message => message.BusinessReason)
            .Should()
            .BeEquivalentTo(new List<string> { expectedBusinessReason1, expectedBusinessReason2, });
    }

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private async Task<ArchivedMessage> CreateArchivedMessageAsync(
        ArchivedMessageType? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? documentType = null,
        string? businessReasons = null,
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
            documentType ?? DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            ActorRole.MeteredDataAdministrator,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            ActorRole.DanishEnergyAgency,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            businessReasons ?? BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageType.IncomingMessage,
            new ArchivedMessageStream(documentStream),
            null);

        await _sut.CreateAsync(archivedMessage, CancellationToken.None);

        return archivedMessage;
    }
}
