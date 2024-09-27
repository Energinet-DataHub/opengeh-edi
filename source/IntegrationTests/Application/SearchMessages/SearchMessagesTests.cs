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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.SearchMessages;

public class SearchMessagesTests : TestBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly IClock _clock;

    public SearchMessagesTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _archivedMessagesClient = GetService<IArchivedMessagesClient>();
        _clock = GetService<IClock>();
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_relation_to_more_than_one_message()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage31 = MessageId.New();
        var messageIdOfMessage32 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage31.Value);
        var archivedMessage32 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage32.Value);
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
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
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
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
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage3, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageIdOfMessage2, messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
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
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageId.Value);
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), relatedToMessageId: messageId, messageId: MessageId.New().Value);
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
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ownActorNumber, restriction: Restriction.Owned, ActorRole.EnergySupplier));

        var archivedMessageOwnMessageAsReceiver = CreateArchivedMessage(_clock.GetCurrentInstant(), receiverNumber: ownActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(_clock.GetCurrentInstant(), receiverNumber: someoneElseActorNumber.Value, senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(_clock.GetCurrentInstant(), receiverNumber: someoneElseActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
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
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ownActorNumber, restriction: Restriction.None, ActorRole.DataHubAdministrator));

        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(_clock.GetCurrentInstant(), receiverNumber: someoneElseActorNumber.Value, senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(_clock.GetCurrentInstant(), receiverNumber: someoneElseActorNumber.Value, senderNumber: someoneElseActorNumber.Value);
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

    [Fact]
    public async Task Given_RequestWithReceiverAndSender_When_RequestingWithinPeriod_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var expectedActorNumber = ActorNumber.Create("1234512345888").Value;
        var startDate = CreatedAt("2023-05-07T22:00:00Z");

        var messageWithinSearchPeriod = CreateArchivedMessage(
            senderNumber: expectedActorNumber,
            receiverNumber: expectedActorNumber,
            createdAt: startDate);
        var messageOutsideSearchPeriod = CreateArchivedMessage(
            senderNumber: expectedActorNumber,
            receiverNumber: expectedActorNumber,
            createdAt: startDate.PlusDays(3));

        await ArchiveMessage(messageWithinSearchPeriod);
        await ArchiveMessage(messageOutsideSearchPeriod);

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                ReceiverNumber: expectedActorNumber,
                SenderNumber: expectedActorNumber,
                CreationPeriod: new MessageCreationPeriod(
                startDate.PlusDays(-2),
                startDate.PlusDays(2))),
            CancellationToken.None);

        // Assert
        using var assertionScope = new AssertionScope();
        result.Messages.Should()
            .ContainSingle()
            .Subject.MessageId
            .Should().Be(messageWithinSearchPeriod.MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_authenticated_actor_role()
    {
        // Arrange
        var authenticatedActorNumber = ActorNumber.Create("1234512345888");
        var authenticatedActorRole = ActorRole.EnergySupplier;
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(authenticatedActorNumber, restriction: Restriction.Owned, authenticatedActorRole));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-04-01T22:00:00Z")));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z"), receiverNumber: authenticatedActorNumber.Value, receiverRole: authenticatedActorRole));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-06-01T22:00:00Z"), senderNumber: authenticatedActorNumber.Value, senderRole: authenticatedActorRole));

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(CreatedAt("2023-05-01T22:00:00Z"), result.Messages[0].CreatedAt);
        Assert.Equal(CreatedAt("2023-06-01T22:00:00Z"), result.Messages[1].CreatedAt);
    }

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private ArchivedMessage CreateArchivedMessage(
        Instant? createdAt = null,
        string? senderNumber = null,
        ActorRole? senderRole = null,
        string? receiverNumber = null,
        ActorRole? receiverRole = null,
        string? documentType = null,
        string? businessReason = null,
        string? messageId = null,
        ArchivedMessageType? archivedMessageType = null,
        MessageId? relatedToMessageId = null)
    {
        return new ArchivedMessage(
            messageId ?? "MessageId",
            Array.Empty<EventId>(),
            documentType ?? DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            ActorRole.EnergySupplier,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            ActorRole.EnergySupplier,
            createdAt.GetValueOrDefault(_clock.GetCurrentInstant()),
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
