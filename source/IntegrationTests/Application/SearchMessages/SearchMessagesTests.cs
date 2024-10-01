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

using System.Reflection;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
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

    public static IEnumerable<object[]> GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy()
    {
        var fieldsToSortBy =
            typeof(FieldToSortBy).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var directionsToSortBy =
            typeof(DirectionToSortBy).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var field in fieldsToSortBy)
        {
            foreach (var direction in directionsToSortBy)
            {
                yield return [field.GetValue(null)!, direction.GetValue(null)!];
            }
        }
    }

    [Fact]
    public async Task Can_fetch_messages()
    {
        var archivedMessage = CreateArchivedMessage(_clock.GetCurrentInstant());
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination()),
            CancellationToken.None);

        var messageInfo = result.Messages.FirstOrDefault(message => message.Id == archivedMessage.Id.Value);
        Assert.NotNull(messageInfo);
        Assert.Equal(archivedMessage.DocumentType, messageInfo.DocumentType);
        Assert.Equal(archivedMessage.SenderNumber.Value, messageInfo.SenderNumber);
        Assert.Equal(archivedMessage.ReceiverNumber.Value, messageInfo.ReceiverNumber);
        Assert.Equal(
            archivedMessage.CreatedAt.ToDateTimeUtc().ToString("u"),
            messageInfo.CreatedAt.ToDateTimeUtc().ToString("u")); // "u" is the "yyyy-mm-dd hh:MM:ssZ" format
        Assert.Equal(archivedMessage.MessageId, messageInfo.MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_creation_date_period()
    {
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-04-01T22:00:00Z")));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-05-01T22:00:00Z")));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-06-01T22:00:00Z")));

        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                new MessageCreationPeriod(
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
                new SortedCursorBasedPagination(),
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
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                MessageId: messageId),
            CancellationToken.None);

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
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                SenderNumber: senderNumber),
            CancellationToken.None);

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
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                ReceiverNumber: receiverNumber),
            CancellationToken.None);

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
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                DocumentTypes: new List<string> { confirmRequestChangeOfSupplier, rejectRequestChangeOfSupplier, }),
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
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                BusinessReasons: new List<string>() { moveIn.Name, balanceFixing.Name, }),
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
    public async Task
        Include_related_messages_when_searching_for_a_message_which_has_a_relation_to_more_than_one_message()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage31 = MessageId.New();
        var messageIdOfMessage32 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: messageIdOfMessage31.Value);
        var archivedMessage32 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: messageIdOfMessage32.Value);
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage2,
            messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a response to a request with more than one response
        var resultForMessageId3 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                MessageId: messageIdOfMessage31.Value,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        resultForMessageId3.Messages.Should().HaveCount(3);
        resultForMessageId3.Messages.Select(m => m.MessageId)
            .Should()
            .BeEquivalentTo(
                new[] { archivedMessage3.MessageId, archivedMessage31.MessageId, archivedMessage32.MessageId, });
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_relation_to_one_message()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage33 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage2,
            messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a request with one response
        var resultForMessageId2 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                MessageId: messageIdOfMessage2.Value,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // Assert
        resultForMessageId2.Messages.Should().HaveCount(2);
        resultForMessageId2.Messages.Select(m => m.MessageId)
            .Should()
            .BeEquivalentTo(
                new[] { archivedMessage2.MessageId, archivedMessage21.MessageId, });
    }

    [Fact]
    public async Task Include_related_messages_when_searching_for_a_message_which_has_a_no_relation()
    {
        // Arrange
        var messageIdOfMessage3 = MessageId.New();
        var messageIdOfMessage33 = MessageId.New();
        var archivedMessage3 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage3.Value);
        var archivedMessage31 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: messageIdOfMessage33.Value);
        var archivedMessage32 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage3,
            messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage3);
        await ArchiveMessage(archivedMessage31);
        await ArchiveMessage(archivedMessage32);

        var messageIdOfMessage2 = MessageId.New();
        var archivedMessage2 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage2.Value);
        var archivedMessage21 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageIdOfMessage2,
            messageId: Guid.NewGuid().ToString());
        await ArchiveMessage(archivedMessage2);
        await ArchiveMessage(archivedMessage21);

        var messageIdOfMessage1 = MessageId.New();
        var archivedMessage1 = CreateArchivedMessage(_clock.GetCurrentInstant(), messageId: messageIdOfMessage1.Value);
        await ArchiveMessage(archivedMessage1);

        // Act
        // This could simulate a search for a message, where it is a request with one response
        var resultForMessageId1 = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
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
        var archivedMessage2 = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            relatedToMessageId: messageId,
            messageId: MessageId.New().Value);
        await ArchiveMessage(archivedMessage1);
        await ArchiveMessage(archivedMessage2);

        var resultForMessageId = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
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
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ownActorNumber, restriction: Restriction.Owned, ActorRole.EnergySupplier));

        var archivedMessageOwnMessageAsReceiver = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            receiverNumber: ownActorNumber.Value,
            senderNumber: someoneElseActorNumber.Value);
        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            receiverNumber: someoneElseActorNumber.Value,
            senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            receiverNumber: someoneElseActorNumber.Value,
            senderNumber: someoneElseActorNumber.Value);
        await ArchiveMessage(archivedMessageOwnMessageAsReceiver);
        await ArchiveMessage(archivedMessageOwnMessageAsSender);
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination()),
            CancellationToken.None);

        Assert.Equal(2, result.Messages.Count);
        Assert.True(
            result.Messages.First().SenderNumber == ownActorNumber.Value
            || result.Messages.First().ReceiverNumber == ownActorNumber.Value);
        Assert.True(
            result.Messages.Last().SenderNumber == ownActorNumber.Value
            || result.Messages.Last().ReceiverNumber == ownActorNumber.Value);
    }

    [Fact]
    public async Task Actor_identity_with_none_restriction_can_fetch_all_messages()
    {
        var ownActorNumber = ActorNumber.Create("1234512345888");
        var someoneElseActorNumber = ActorNumber.Create("1234512345777");
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ownActorNumber, restriction: Restriction.None, ActorRole.DataHubAdministrator));

        var archivedMessageOwnMessageAsSender = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            receiverNumber: someoneElseActorNumber.Value,
            senderNumber: ownActorNumber.Value);
        var archivedMessage = CreateArchivedMessage(
            _clock.GetCurrentInstant(),
            receiverNumber: someoneElseActorNumber.Value,
            senderNumber: someoneElseActorNumber.Value);
        await ArchiveMessage(archivedMessageOwnMessageAsSender);
        await ArchiveMessage(archivedMessage);

        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination()),
            CancellationToken.None);

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
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
                ReceiverNumber: expectedActorNumber,
                SenderNumber: expectedActorNumber),
            CancellationToken.None);

        //Assert
        var receiverAndSenderNumber = result.Messages.Select(m => m.ReceiverNumber)
            .Intersect(result.Messages.Select(m => m.SenderNumber));
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
                new SortedCursorBasedPagination(),
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
            .Should()
            .Be(messageWithinSearchPeriod.MessageId);
    }

    [Fact]
    public async Task Filter_messages_by_authenticated_actor_role()
    {
        // Arrange
        var authenticatedActorNumber = ActorNumber.Create("1234512345888");
        var authenticatedActorRole = ActorRole.EnergySupplier;
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(authenticatedActorNumber, restriction: Restriction.Owned, authenticatedActorRole));
        await ArchiveMessage(CreateArchivedMessage(CreatedAt("2023-04-01T22:00:00Z")));
        await ArchiveMessage(
            CreateArchivedMessage(
                CreatedAt("2023-05-01T22:00:00Z"),
                receiverNumber: authenticatedActorNumber.Value,
                receiverRole: authenticatedActorRole));
        await ArchiveMessage(
            CreateArchivedMessage(
                CreatedAt("2023-06-01T22:00:00Z"),
                senderNumber: authenticatedActorNumber.Value,
                senderRole: authenticatedActorRole));

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination()),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Messages.Count);
        Assert.Equal(CreatedAt("2023-06-01T22:00:00Z"), result.Messages[0].CreatedAt);
        Assert.Equal(CreatedAt("2023-05-01T22:00:00Z"), result.Messages[1].CreatedAt);
    }

    [Fact]
    public async Task Can_fetch_messages_with_pagination_navigation_forward_returns_expected_messages()
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination(pageSize: 5, navigationForward: true)),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(5);
        var expectedFirst5MessageIds = messages
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => x.MessageId)
            .ToList();

        Assert.Equal(expectedFirst5MessageIds, result.Messages.Select(x => x.MessageId));
    }

    [Theory]
    [InlineData(-8)]
    [InlineData(-1)]
    [InlineData(0)]
    public Task Can_not_fetch_messages_with_invalid_page_size(int pageSize)
    {
        // Arrange
        // Act
        var act = () => new SortedCursorBasedPagination(pageSize: pageSize, navigationForward: true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task Can_fetch_all_messages_with_pagination_navigation_forward_returns_expected_messages(int pageSize)
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: pageSize,
            navigationForward: true);

        // Act and Assert
        var skip = 0;
        var nextPage = true;
        while (nextPage)
        {
            var result = await _archivedMessagesClient.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

            if (result.Messages.Count < pageSize)
            {
                nextPage = false;
            }
            else
            {
                // use the last message as the cursor when navigating forward
                var lastMessage = result.Messages.Last();
                pagination = new SortedCursorBasedPagination(
                    cursor: new SortingCursor(
                        SortedFieldValue: lastMessage.CreatedAt.ToString(),
                        RecordId: lastMessage.RecordId),
                    pageSize: pageSize,
                    navigationForward: true);
            }

            if (nextPage)
            {
                result.Messages.Should().HaveCount(pageSize);
            }
            else
            {
                result.Messages.Should().HaveCount(messages.Count % pageSize);
            }

            var expectedMessageIds = messages
                // Default sorting is descending by CreatedAt
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(pageSize)
                .Select(x => x.MessageId)
                .ToList();

            Assert.Equal(expectedMessageIds, result.Messages.Select(x => x.MessageId));

            skip += pageSize;
        }
    }

    [Fact]
    public async Task Can_fetch_messages_with_pagination_navigation_backward_returns_expected_messages()
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        // Act
        var result = await _archivedMessagesClient.SearchAsync(
            new GetMessagesQuery(new SortedCursorBasedPagination(pageSize: 5, navigationForward: false)),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(5);
        var expectedLast5MessageIds = messages
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Skip(1)
            .Take(5)
            .Select(x => x.MessageId)
            .ToList();

        Assert.Equal(expectedLast5MessageIds, result.Messages.Select(x => x.MessageId));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public async Task Can_fetch_all_messages_with_pagination_navigation_backward_returns_expected_messages(int pageSize)
    {
        // Arrange
        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        var pagination = new SortedCursorBasedPagination(pageSize: pageSize, navigationForward: false);

        // Act
        var skip = 0;
        var nextPage = true;
        while (nextPage)
        {
            var result = await _archivedMessagesClient.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

            if (result.Messages.Count < pageSize)
            {
                nextPage = false;
            }
            else
            {
                // use the first message as the cursor when navigating forward
                var firstMessage = result.Messages.First();
                pagination = new SortedCursorBasedPagination(
                    cursor: new SortingCursor(
                        SortedFieldValue: firstMessage.CreatedAt.ToString(),
                        RecordId: firstMessage.RecordId),
                    pageSize: pageSize,
                    navigationForward: false);
            }

            // Assert
            if (nextPage)
            {
                result.Messages.Should().HaveCount(pageSize);
            }
            else
            {
                result.Messages.Should().HaveCount(messages.Count % pageSize);
            }

            var expectedMessageIds = messages
                .Skip(skip)
                .Take(pageSize)
                .ToList()
                // Default sorting is descending by CreatedAt
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.MessageId)
                .ToList();

            Assert.Equal(expectedMessageIds, result.Messages.Select(x => x.MessageId));

            skip += pageSize;
        }
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task Can_fetch_all_messages_with_pagination_navigation_forward_and_sorted_by_returns_expected_messages(
        FieldToSortBy sortedBy,
        DirectionToSortBy sortedDirection)
    {
        // Arrange
        var pageSize = 2;
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)>()
            {
                new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345128", "1234512345122", DocumentType.NotifyAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345127", "1234512345123", DocumentType.NotifyWholesaleServices.Name),
                new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345125", "1234512345121", DocumentType.RejectRequestAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345124", "1234512345126", DocumentType.NotifyAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123", "1234512345128", DocumentType.RejectRequestWholesaleSettlement.Name),
                new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345122", "1234512345127", DocumentType.NotifyAggregatedMeasureData.Name),
            };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: pageSize,
            navigationForward: true,
            fieldToSortBy: sortedBy,
            directionSortBy: sortedDirection);

        var nextPage = true;

        // Act
        var searchResults = new List<MessageInfo>();
        while (nextPage)
        {
            var result = await _archivedMessagesClient.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

            if (result.Messages.Count < pageSize)
            {
                nextPage = false;
            }
            else
            {
                // use the last message as the cursor when navigating forward
                var lastMessage = result.Messages.Last();
                pagination = new SortedCursorBasedPagination(
                    cursor: new SortingCursor(
                        SortedFieldValue: lastMessage.GetType().GetProperty(sortedBy.Identifier)!.GetValue(lastMessage)!
                            .ToString(),
                        RecordId: lastMessage.RecordId),
                    pageSize: pageSize,
                    navigationForward: true,
                    fieldToSortBy: sortedBy,
                    directionSortBy: sortedDirection);
            }

            searchResults.AddRange(result.Messages);
        }

        // Assert
        searchResults.Should().HaveCount(messages.Count);
        var orderedMessages = GetOrderedMessagedAfterSorting(sortedBy, sortedDirection, messages);

        searchResults.Select(x => x.MessageId)
            .Should()
            .BeEquivalentTo(orderedMessages.Select(x => x.MessageId));
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task Can_fetch_all_messages_with_pagination_navigation_backward_and_sorted_by_returns_expected_messages(
        FieldToSortBy sortedBy,
        DirectionToSortBy sortedDirection)
    {
        // Arrange
        var pageSize = 2;
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)>()
            {
                new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345128", "1234512345122", DocumentType.NotifyAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345127", "1234512345123", DocumentType.NotifyWholesaleServices.Name),
                new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345125", "1234512345121", DocumentType.RejectRequestAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345124", "1234512345126", DocumentType.NotifyAggregatedMeasureData.Name),
                new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345123", "1234512345128", DocumentType.RejectRequestWholesaleSettlement.Name),
                new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString(), "1234512345122", "1234512345127", DocumentType.NotifyAggregatedMeasureData.Name),
            };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            var archivedMessage = CreateArchivedMessage(
                exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
            await ArchiveMessage(archivedMessage);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: pageSize,
            navigationForward: false,
            fieldToSortBy: sortedBy,
            directionSortBy: sortedDirection);

        var nextPage = true;

        // Act
        var searchResults = new List<MessageInfo>();
        while (nextPage)
        {
            var result = await _archivedMessagesClient.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

            if (result.Messages.Count < pageSize)
            {
                nextPage = false;
            }
            else
            {
                // use the first message as the cursor when navigating backward
                var firstMessage = result.Messages.First();
                pagination = new SortedCursorBasedPagination(
                    cursor: new SortingCursor(
                        SortedFieldValue: firstMessage.GetType().GetProperty(sortedBy.Identifier)!.GetValue(firstMessage)!
                            .ToString(),
                        RecordId: firstMessage.RecordId),
                    pageSize: pageSize,
                    navigationForward: false,
                    fieldToSortBy: sortedBy,
                    directionSortBy: sortedDirection);
            }

            searchResults.AddRange(result.Messages);
        }

        // Assert
        searchResults.Should().HaveCount(messages.Count);
        var orderedMessages = GetOrderedMessagedAfterSorting(sortedBy, sortedDirection, messages);

        searchResults.Select(x => x.MessageId)
            .Should()
            .BeEquivalentTo(orderedMessages.Select(x => x.MessageId));
    }

    private static IOrderedEnumerable<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)> GetOrderedMessagedAfterSorting(
        FieldToSortBy sortedBy,
        DirectionToSortBy sortedDirection,
        List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)> messages)
    {
        var orderedMessages = messages.Order();
        if (sortedBy.Identifier == FieldToSortBy.MessageId.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.MessageId)
                : messages.OrderByDescending(x => x.MessageId);
        }

        if (sortedBy.Identifier == FieldToSortBy.CreatedAt.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.CreatedAt)
                : messages.OrderByDescending(x => x.CreatedAt);
        }

        if (sortedBy.Identifier == FieldToSortBy.DocumentType.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.DocumentType)
                : messages.OrderByDescending(x => x.DocumentType);
        }

        if (sortedBy.Identifier == FieldToSortBy.SenderNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.Sender)
                : messages.OrderByDescending(x => x.Sender);
        }

        if (sortedBy.Identifier == FieldToSortBy.ReceiverNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.Receiver)
                : messages.OrderByDescending(x => x.Receiver);
        }

        return orderedMessages;
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
            messageId ?? Guid.NewGuid().ToString(),
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
