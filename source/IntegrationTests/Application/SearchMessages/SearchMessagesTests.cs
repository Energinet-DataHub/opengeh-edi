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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using FluentAssertions;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.SearchMessages;

public class SearchMessagesTests : TestBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public SearchMessagesTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _archivedMessagesClient = GetService<IArchivedMessagesClient>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
    }

    [Fact]
    public async Task Can_fetch_messages()
    {
        var archivedMessage = CreateArchivedMessage(_systemDateTimeProvider.Now());
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        var messageInfo = result.Messages.FirstOrDefault(message => message.Id == archivedMessage.Id.Value);
        Assert.NotNull(messageInfo);
        Assert.Equal(archivedMessage.DocumentType, messageInfo.DocumentType);
        Assert.Equal(archivedMessage.SenderNumber, messageInfo.SenderNumber);
        Assert.Equal(archivedMessage.ReceiverNumber, messageInfo.ReceiverNumber);
        Assert.Equal(archivedMessage.CreatedAt.ToDateTimeUtc().ToString("u"), messageInfo.CreatedAt.ToDateTimeUtc().ToString("u")); // "u" is the "yyyy-mm-dd hh:MM:ssZ" format
        Assert.Equal(archivedMessage.MessageId, messageInfo.MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_creation_date_period()
    {
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-04-01T22:00:00Z")));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z")));

        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new MessageCreationPeriod(
            CreatedAt("2023-05-01T22:00:00Z"),
            CreatedAt("2023-05-02T22:00:00Z"))),
            CancellationToken.None);

        Assert.Single(result.Messages);
        Assert.Equal(CreatedAt("2023-05-01T22:00:00Z"), result.Messages[0].CreatedAt);
    }

    [Fact]
    public async Task Filter_messages_by_message_id_and_created_date()
    {
        //Arrange
        var messageId = Guid.NewGuid().ToString();
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z"), messageId: messageId));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:01Z")));

        //Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new MessageCreationPeriod(
                CreatedAt("2023-05-01T22:00:00Z"),
                CreatedAt("2023-05-02T22:00:01Z")),
                messageId),
            CancellationToken.None);

        //Assert
        Assert.Single(result.Messages);
        Assert.Equal(messageId, result.Messages[0].MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_message_id()
    {
        //Arrange
        var messageId = Guid.NewGuid().ToString();
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z"), messageId: messageId));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z")));

        //Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(MessageId: messageId), CancellationToken.None);

        //Assert
        Assert.Single(result.Messages);
        Assert.Equal(messageId, result.Messages[0].MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_sender_number()
    {
        //Arrange
        var senderNumber = "1234512345128";
        await ArchiveMessage(CreateArchivedMessage(senderNumber: senderNumber));
        await ArchiveMessage(CreateArchivedMessage());

        //Act
        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(SenderNumber: senderNumber), CancellationToken.None);

        //Assert
        Assert.Single(result.Messages);
        Assert.Equal(senderNumber, result.Messages[0].SenderNumber);
    }

    [Fact]
    public async Task Filter_messages_by_receiver()
    {
        // Arrange
        var receiverNumber = "1234512345129";
        await ArchiveMessage(CreateArchivedMessage(receiverNumber: receiverNumber));
        await ArchiveMessage(CreateArchivedMessage());

        // Act
        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(ReceiverNumber: receiverNumber), CancellationToken.None);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal(receiverNumber, result.Messages[0].ReceiverNumber);
    }

    [Fact]
    public async Task Filter_messages_by_document_types()
    {
        // Arrange
        var confirmRequestChangeOfSupplier = DocumentType.NotifyAggregatedMeasureData.Name;
        var rejectRequestChangeOfSupplier = DocumentType.RejectRequestAggregatedMeasureData.Name;
        await ArchiveMessage(CreateArchivedMessage(documentType: confirmRequestChangeOfSupplier));
        await ArchiveMessage(CreateArchivedMessage(documentType: rejectRequestChangeOfSupplier));
        await ArchiveMessage(CreateArchivedMessage());

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(DocumentTypes: new List<string>
            {
            confirmRequestChangeOfSupplier,
            rejectRequestChangeOfSupplier,
            }),
            CancellationToken.None);

        // Assert
        Assert.Contains(
            result.Messages,
            message => message.DocumentType.Equals(confirmRequestChangeOfSupplier, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Messages,
            message => message.DocumentType.Equals(rejectRequestChangeOfSupplier, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Filter_messages_by_business_reasons()
    {
        // Arrange
        var moveIn = BusinessReason.MoveIn;
        var balanceFixing = BusinessReason.BalanceFixing;
        await ArchiveMessage(CreateArchivedMessage(businessReason: moveIn.Name));
        await ArchiveMessage(CreateArchivedMessage(businessReason: balanceFixing.Name));
        await ArchiveMessage(CreateArchivedMessage());

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(BusinessReasons: new List<string>()
        {
            moveIn.Name,
            balanceFixing.Name,
        }),
            CancellationToken.None);

        // Assert
        Assert.Contains(
            result.Messages,
            message => message.BusinessReason!.Equals(moveIn.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            result.Messages,
            message => message.BusinessReason!.Equals(balanceFixing.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_relation_to_more_than_one_message()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage31 = MessageId.New();
        var messageIdOfMessage32 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage31.Value);
        var archivedMessage32 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage32.Value);
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a response to a request with more than one response
        var resultForMessageId3 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageIdOfMessage31.Value,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        resultForMessageId3.Messages.Should().HaveCount(3);
        resultForMessageId3.Messages.Select(m => m.MessageId)
            .Should()
            .BeEquivalentTo(
                new[]
                {
                    archivedMessage3.MessageId,
                    archivedMessage31.MessageId,
                    archivedMessage32.MessageId,
                });
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_relation_to_one_message()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage33 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a request with one response
        var resultForMessageId2 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageIdOfMessage2.Value,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        resultForMessageId2.Messages.Should().HaveCount(2);
        resultForMessageId2.Messages.Select(m => m.MessageId)
            .Should()
            .BeEquivalentTo(
                new[]
                    {
                        archivedMessage2.MessageId,
                        archivedMessage21.MessageId,
                    });
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_no_relation()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage33 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage3, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a request with one response
        var resultForMessageId1 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageIdOfMessage1.Value,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        resultForMessageId1.Messages.Should().ContainSingle().Which.MessageId.Should().Be(archivedMessage1.MessageId);
    }

    [Fact]
    public async Task Related_messages_are_not_included_when_IncludeRelatedMessages_is_false()
    {
        // Arrange
        var messageId = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_systemDateTimeProvider.Now(), messageId: messageId.Value);
        var archivedMessage2 = CreateArchivedMessage(_systemDateTimeProvider.Now(), relatedToMessageId: messageId, messageId: MessageId.New().Value);
        await ArchiveMessage(archivedMessage1);
        await ArchiveMessage(archivedMessage2);

        var resultForMessageId = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                MessageId: messageId.Value,
                IncludeRelatedMessages: false),
            CancellationToken.None);

        resultForMessageId.Messages.Should().ContainSingle().Which.MessageId.Should().Be(archivedMessage1.MessageId);
    }

    [Fact]
    public async Task Actor_identity_with_owned_restriction_can_only_fetch_own_messages()
    {
        var ownActorNumber = ActorNumber.Create("1234512345888");
        var someoneElseActorNumber = ActorNumber.Create("1234512345777");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ownActorNumber, restriction: Restriction.Owned));

        var archivedMessageOwnMessageAsReceiver = CreateArchivedMessage(_systemDateTimeProvider.Now(), receiverNumber: ownActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(_systemDateTimeProvider.Now(), receiverNumber: someoneElseActorNumber.Value, senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(_systemDateTimeProvider.Now(), receiverNumber: someoneElseActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
        await ArchiveMessage(archivedMessageOwnMessageAsReceiver);
        await ArchiveMessage(archivedMessageOwnMessageAsSender);
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.True(result.Messages.First().SenderNumber == ownActorNumber.Value || result.Messages.First().ReceiverNumber == ownActorNumber.Value);
        Assert.True(result.Messages.Last().SenderNumber == ownActorNumber.Value || result.Messages.Last().ReceiverNumber == ownActorNumber.Value);
    }

    [Fact]
    public async Task Actor_identity_with_none_restriction_can_fetch_all_messages()
    {
        var ownActorNumber = ActorNumber.Create("1234512345888");
        var someoneElseActorNumber = ActorNumber.Create("1234512345777");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ownActorNumber, restriction: Restriction.None));

        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(_systemDateTimeProvider.Now(), receiverNumber: someoneElseActorNumber.Value, senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(_systemDateTimeProvider.Now(), receiverNumber: someoneElseActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
        await ArchiveMessage(archivedMessageOwnMessageAsSender);
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(new GetMessagesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
    }

    [Fact]
    public async Task Filter_messages_by_receiver_number_and_sender_number()
    {
        //Arrange
        var expectedActorNumber = ActorNumber.Create("1234512345888").Value;
        await ArchiveMessage(CreateArchivedMessage(senderNumber: expectedActorNumber));
        await ArchiveMessage(CreateArchivedMessage(receiverNumber: expectedActorNumber));

        //Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(ReceiverNumber: expectedActorNumber, SenderNumber: expectedActorNumber),
            CancellationToken.None);

        //Assert
        var receiverAndSenderNumber = result.Messages.Select(m => m.ReceiverNumber).Intersect(result.Messages.Select(m => m.SenderNumber));
        foreach (var actualActorNumber in receiverAndSenderNumber)
        {
            Assert.Equal(expectedActorNumber, actualActorNumber);
        }
    }

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private ArchivedMessage CreateArchivedMessage(
        Instant? createdAt = null,
        string? senderNumber = null,
        string? receiverNumber = null,
        string? documentType = null,
        string? businessReason = null,
        string? messageId = null,
        ArchivedMessageType? archivedMessageType = null,
        MessageId? relatedToMessageId = null)
    {
        return new ArchivedMessage(
            messageId ?? "MessageId",
            documentType ?? DocumentType.NotifyAggregatedMeasureData.Name,
            senderNumber ?? "1234512345123",
            receiverNumber ?? "1234512345128",
            createdAt.GetValueOrDefault(_systemDateTimeProvider.Now()),
            businessReason ?? BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageType.OutgoingMessage,
#pragma warning disable CA2000 // Do not dispose here
            new MarketDocumentStream(new MarketDocumentWriterMemoryStream()),
#pragma warning restore CA2000
            relatedToMessageId);
    }

    private async Task ArchiveMessage(ArchivedMessage archivedMessage)
    {
        await _archivedMessagesClient.CreateAsync(archivedMessage, CancellationToken.None);
    }
}
