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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class SearchMessagesWithoutRestrictionTests
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    private readonly ActorIdentity _authenticatedActor = new(
        ActorNumber.Create("1234512345811"),
        Restriction.None,
        ActorRole.MeteredDataAdministrator);

    public SearchMessagesWithoutRestrictionTests(ArchivedMessagesFixture fixture)
    {
        _fixture = fixture;

        fixture.AuthenticatedActor.SetAuthenticatedActor(_authenticatedActor);
        _sut = fixture.ArchivedMessagesClient;
        _fixture.CleanupDatabase();
        _fixture.CleanupFileStorage();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_SearchingWithoutCriteria_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        var message = result.Messages.Should().ContainSingle().Subject;
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.SenderNumber.Should()
            .Be(archivedMessage.SenderNumber.Value)
            .And.NotBe(_authenticatedActor.ActorNumber.Value);
        message.ReceiverNumber.Should()
            .Be(archivedMessage.ReceiverNumber.Value)
            .And.NotBe(_authenticatedActor.ActorNumber.Value);
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
        await _fixture.CreateArchivedMessageAsync(timestamp: expectedCreatedAt.PlusDays(-1));
        await _fixture.CreateArchivedMessageAsync(timestamp: expectedCreatedAt);
        await _fixture.CreateArchivedMessageAsync(timestamp: expectedCreatedAt.PlusDays(1));

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                new MessageCreationPeriod(
                    expectedCreatedAt.PlusHours(-2),
                    expectedCreatedAt.PlusHours(2))),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Messages.Should().ContainSingle()
            .Which.CreatedAt.Should().Be(expectedCreatedAt);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingBySenderNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedSenderNumber = "9999999999999";
        await _fixture.CreateArchivedMessageAsync(senderNumber: expectedSenderNumber);
        await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                SenderNumber: expectedSenderNumber),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.SenderNumber.Should().Be(expectedSenderNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByReceiverNumber_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedReceiverNumber = "9999999999999";
        await _fixture.CreateArchivedMessageAsync(receiverNumber: expectedReceiverNumber);
        await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                ReceiverNumber: expectedReceiverNumber),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.ReceiverNumber.Should().Be(expectedReceiverNumber);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByMessageId_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        await _fixture.CreateArchivedMessageAsync(messageId: expectedMessageId);
        await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: expectedMessageId),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.MessageId.Should().Be(expectedMessageId);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByDocumentType_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedDocumentType = DocumentType.NotifyAggregatedMeasureData.Name;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData.Name;
        await _fixture.CreateArchivedMessageAsync(documentType: expectedDocumentType);
        await _fixture.CreateArchivedMessageAsync(documentType: unexpectedDocumentType);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                DocumentTypes: [expectedDocumentType]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.DocumentType.Should().Be(expectedDocumentType);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByDocumentTypes_Then_ReturnsExpectedMessages()
    {
        // Arrange
        var expectedDocumentType1 = DocumentType.NotifyAggregatedMeasureData.Name;
        var expectedDocumentType2 = DocumentType.NotifyWholesaleServices.Name;
        var unexpectedDocumentType = DocumentType.RejectRequestAggregatedMeasureData.Name;
        await _fixture.CreateArchivedMessageAsync(documentType: expectedDocumentType1);
        await _fixture.CreateArchivedMessageAsync(documentType: expectedDocumentType2);
        await _fixture.CreateArchivedMessageAsync(documentType: unexpectedDocumentType);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                DocumentTypes:
                [
                    expectedDocumentType1,
                    expectedDocumentType2,
                ]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().HaveCount(2);
        result.Messages.Select(message => message.DocumentType)
            .Should()
            .BeEquivalentTo(
            [
                expectedDocumentType1,
                expectedDocumentType2,
            ]);
    }

    [Fact]
    public async Task Given_TwoArchivedMessages_When_SearchingByBusinessReason_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedBusinessReason = BusinessReason.MoveIn.Name;
        var unexpectedBusinessReason = BusinessReason.BalanceFixing.Name;
        await _fixture.CreateArchivedMessageAsync(businessReasons: expectedBusinessReason);
        await _fixture.CreateArchivedMessageAsync(businessReasons: unexpectedBusinessReason);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                BusinessReasons: [expectedBusinessReason]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.BusinessReason.Should().Be(expectedBusinessReason);
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingByBusinessReasons_Then_ReturnsExpectedMessages()
    {
        // Arrange
        var expectedBusinessReason1 = BusinessReason.MoveIn.Name;
        var expectedBusinessReason2 = BusinessReason.Correction.Name;
        var unexpectedBusinessReason = BusinessReason.BalanceFixing.Name;
        await _fixture.CreateArchivedMessageAsync(businessReasons: expectedBusinessReason1);
        await _fixture.CreateArchivedMessageAsync(businessReasons: expectedBusinessReason2);
        await _fixture.CreateArchivedMessageAsync(businessReasons: unexpectedBusinessReason);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                BusinessReasons:
                [
                    expectedBusinessReason1,
                    expectedBusinessReason2,
                ]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().HaveCount(2);
        result.Messages.Select(message => message.BusinessReason)
            .Should()
            .BeEquivalentTo(
            [
                expectedBusinessReason1,
                expectedBusinessReason2,
            ]);
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_SearchingByAllCriteria_Then_AllCriteriaAreMet()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        var expectedCreatedAt = CreatedAt("2023-05-01T22:00:00Z");
        var expectedSenderNumber = "9999999999999";
        var expectedReceiverNumber = "9999999999998";
        var expectedDocumentType = DocumentType.NotifyWholesaleServices.Name;
        var expectedBusinessReason = BusinessReason.MoveIn.Name;

        var expectedMessage = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber,
            documentType: expectedDocumentType,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingId = await _fixture.CreateArchivedMessageAsync(
            messageId: Guid.NewGuid().ToString(),
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber,
            documentType: expectedDocumentType,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingTimestamp = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt.PlusDays(-2),
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber,
            documentType: expectedDocumentType,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingSenderNumber = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber + "999",
            receiverNumber: expectedReceiverNumber,
            documentType: expectedDocumentType,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingReceiverNumber = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber + "999",
            documentType: expectedDocumentType,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingDocumentType = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber,
            documentType: DocumentType.RejectRequestAggregatedMeasureData.Name,
            businessReasons: expectedBusinessReason);
        var messageWithMismatchingBusinessReason = await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            timestamp: expectedCreatedAt,
            senderNumber: expectedSenderNumber,
            receiverNumber: expectedReceiverNumber,
            documentType: expectedDocumentType,
            businessReasons: BusinessReason.PreliminaryAggregation.Name);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: expectedMessageId,
                CreationPeriod: new MessageCreationPeriod(
                    expectedCreatedAt.PlusHours(-2),
                    expectedCreatedAt.PlusHours(2)),
                SenderNumber: expectedSenderNumber,
                ReceiverNumber: expectedReceiverNumber,
                DocumentTypes: [expectedDocumentType],
                BusinessReasons: [expectedBusinessReason]),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();

        // note that we do have a OR-statement between sender and receiver number. hence we have 3 messages
        result.Messages.Should().HaveCount(3);
        result.Messages.Should()
            .AllSatisfy(
                message =>
                {
                    message.MessageId.Should().Be(expectedMessageId);
                    message.CreatedAt.Should().Be(expectedCreatedAt);
                    message.DocumentType.Should().Be(expectedDocumentType);
                    message.BusinessReason.Should().Be(expectedBusinessReason);
                });
        result.Messages.Select(message => message.SenderNumber)
            .Should()
            .BeEquivalentTo(
            [
                expectedSenderNumber,
                messageWithMismatchingReceiverNumber.SenderNumber.Value,
                messageWithMismatchingSenderNumber.SenderNumber.Value,
            ]);
        result.Messages.Select(message => message.ReceiverNumber)
            .Should()
            .BeEquivalentTo(
            [
                expectedReceiverNumber,
                messageWithMismatchingReceiverNumber.ReceiverNumber.Value,
                messageWithMismatchingSenderNumber.ReceiverNumber.Value,
            ]);
    }

    #region include_related_messages

    [Fact]
    public async Task Given_TwoArchivedMessagesWithRelation_When_ExcludingRelatedMessagesAndSearchingByMessageId_Then_RelatedMessagesAreNotReturned()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            archivedMessageType: ArchivedMessageType.IncomingMessage);
        await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(expectedMessageId),
            archivedMessageType: ArchivedMessageType.OutgoingMessage);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: expectedMessageId,
                IncludeRelatedMessages: false),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should().ContainSingle()
            .Which.MessageId.Should().Be(expectedMessageId);
    }

    [Fact]
    public async Task Given_FourArchivedMessagesWithRelations_When_IncludingRelatedMessagesAndSearchingByMessageId_Then_RelatedMessagesAreReturned()
    {
        // Arrange
        var messageWithoutRelation = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: null,
            archivedMessageType: ArchivedMessageType.IncomingMessage);
        var messageWithRelation = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(messageWithoutRelation.MessageId!),
            archivedMessageType: ArchivedMessageType.OutgoingMessage);
        var messageWithRelation2 = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(messageWithoutRelation.MessageId!),
            archivedMessageType: ArchivedMessageType.OutgoingMessage);
        var unexpectedMessage = await _fixture.CreateArchivedMessageAsync();

        // Act
        // This could simulate a search for a message, where the message is a request with two responses
        var searchForRequest = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageWithoutRelation.MessageId,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // This could simulate a search for a message, where the message is a response to a request with two responses
        var searchForResponse = await _sut.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageWithRelation.MessageId,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        using var assertionScope = new AssertionScope();
        searchForRequest.Messages.Should().HaveCount(3);
        searchForRequest.Should().BeEquivalentTo(searchForResponse); // Note that they are sorted differently
        searchForRequest.Messages.Select(m => m.Id)
            .Should()
            .BeEquivalentTo(
            [
                messageWithoutRelation.Id.Value,
                messageWithRelation.Id.Value,
                messageWithRelation2.Id.Value,
            ]);
    }

    #endregion

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }
}
