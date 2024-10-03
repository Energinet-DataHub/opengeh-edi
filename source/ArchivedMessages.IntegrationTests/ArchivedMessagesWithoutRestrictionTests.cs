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
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class ArchivedMessagesWithoutRestrictionTests : IAsyncLifetime
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    private readonly ActorIdentity _authenticatedActor = new(
        ActorNumber.Create("1234512345811"),
        Restriction.None,
        ActorRole.MeteredDataAdministrator);

    public ArchivedMessagesWithoutRestrictionTests(ArchivedMessagesFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;

        var services = _fixture.BuildService(testOutputHelper);

        services.GetRequiredService<AuthenticatedActor>().SetAuthenticatedActor(_authenticatedActor);
        _sut = services.GetRequiredService<IArchivedMessagesClient>();
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
    public async Task Given_ArchivedMessage_When_Creating_Then_MessageIsStoredInDatabaseAndBlob()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            archivedMessageType: ArchivedMessageType.IncomingMessage,
            storeMessage: false);

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
        var dbResult = await _fixture.GetAllMessagesInDatabase();

        var message = dbResult.Single();
        using var assertionScope = new AssertionScope();

        message.SenderNumber.Should().Be(archivedMessage.SenderNumber.Value);
        message.SenderRoleCode.Should().Be(archivedMessage.SenderRole.Code);
        message.ReceiverNumber.Should().Be(archivedMessage.ReceiverNumber.Value);
        message.ReceiverRoleCode.Should().Be(archivedMessage.ReceiverRole.Code);
        message.DocumentType.Should().Be(archivedMessage.DocumentType);
        message.BusinessReason.Should().Be(archivedMessage.BusinessReason);
        message.MessageId.Should().Be(archivedMessage.MessageId);
        message.FileStorageReference.Should().Be(expectedBlocReference.Path);
        message.RelatedToMessageId.Should().BeNull();
        message.EventIds.Should().BeNull();

        var blobResult = await _fixture.GetMessagesFromBlob(expectedBlocReference);
        blobResult.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ArchivedMessagesInStorage_When_GettingMessage_Then_StreamExists()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.GetAsync(archivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ArchivedMessage_When_SearchingWithoutCriteria_Then_ReturnsExpectedMessage()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync();

        // Act
        var result = await _sut.SearchAsync(new GetMessagesQuery(new SortedCursorBasedPagination()), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalAmountOfMessages.Should().Be(1);
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
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
                new SortedCursorBasedPagination(),
                MessageId: messageWithoutRelation.MessageId,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // This could simulate a search for a message, where the message is a response to a request with two responses
        var searchForResponse = await _sut.SearchAsync(
            new GetMessagesQuery(
                new SortedCursorBasedPagination(),
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

    #region pagination
    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrue_Then_ExpectedMessageAreReturned()
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
            new(CreatedAt("2023-04-07T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var exceptedMessage in messages.OrderBy(_ => Random.Shared.Next()))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: exceptedMessage.CreatedAt,
                messageId: exceptedMessage.MessageId);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: 10,
            navigationForward: true);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(pagination),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);

        var expectedMessageIds = messages
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();
        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingBackwardIsTrue_Then_ExpectedMessageAreReturned()
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
            new(CreatedAt("2023-04-07T22:00:00Z"), Guid.NewGuid().ToString()),
        };
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: 10,
            navigationForward: false);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(pagination),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);

        var expectedMessageIds = messages
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();
        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Theory]
    [InlineData(-8)]
    [InlineData(-1)]
    [InlineData(0)]
    public Task Given_SevenArchivedMessages_When_PageSizeIsInvalid_Then_ExpectedMessageAreReturned(int pageSize)
    {
        // Arrange
        // Act
        var act = () => new SortedCursorBasedPagination(pageSize: pageSize, navigationForward: true);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Page size must be a positive number. (Parameter 'pageSize')");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrueAndSecondPage_Then_ExpectedMessageAreReturned()
    {
        // Arrange
        var pageSize = 2;
        var pageNumber = 2;

        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            // page 1 <- the previous page
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()), // <- cursor points here
            // page 2 <- the page to fetch
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 3
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
        };

        // Create messages in order when they were created at
        foreach (var messageToCreate in messages.OrderBy(x => x.CreatedAt))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId);
        }

        var firstPageMessages = await SkipFirstPage(pageSize, navigatingForward: true);
        // The cursor points at the last item of the previous page, when navigating backward
        var lastMessageInOnThePreviousPage = firstPageMessages.Messages.Last();
        var cursor = new SortingCursor(RecordId: lastMessageInOnThePreviousPage.RecordId);

        var pagination = new SortedCursorBasedPagination(
            cursor: cursor,
            pageSize: pageSize,
            navigationForward: true);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(pagination),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(pageSize);

        var expectedMessageIds = messages
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageSize * pageNumber) - pageSize)
            .Take(pageSize)
            .Select(x => x.MessageId)
            .ToList();

        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Fact]
    public async Task Given_SevenArchivedMessages_When_NavigatingBackwardIsTrueAndSecondPage_Then_ExpectedMessageAreReturned()
    {
        // Arrange
        var pageSize = 2;
        var pageNumber = 2;

        var messages = new List<(Instant CreatedAt, string MessageId)>()
        {
            // page 1
            new(CreatedAt("2023-04-06T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-05T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 2 <- the page to fetch
            new(CreatedAt("2023-04-04T22:00:00Z"), Guid.NewGuid().ToString()),
            new(CreatedAt("2023-04-03T22:00:00Z"), Guid.NewGuid().ToString()),
            // page 3 <- the previous page
            new(CreatedAt("2023-04-02T22:00:00Z"), Guid.NewGuid().ToString()), // <- cursor points here
            new(CreatedAt("2023-04-01T22:00:00Z"), Guid.NewGuid().ToString()),
        };

        // Create messages in order when they were created at
        foreach (var messageToCreate in messages.OrderBy(x => x.CreatedAt))
        {
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId);
        }

        var firstPageMessages = await SkipFirstPage(pageSize, navigatingForward: false);
        // The cursor points at the first item of the previous page, when navigating backward
        var firstMessageInOnThePreviousPage = firstPageMessages.Messages.First();
        var cursor = new SortingCursor(RecordId: firstMessageInOnThePreviousPage.RecordId);

        var pagination = new SortedCursorBasedPagination(
            cursor: cursor,
            pageSize: pageSize,
            navigationForward: false);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQuery(pagination),
            CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(pageSize);

        var expectedMessageIds = messages
            .Skip((pageSize * pageNumber) - pageSize)
            .Take(pageSize)
            // Default sorting is descending by CreatedAt
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.MessageId)
            .ToList();

        result.Messages.Select(x => x.MessageId).Should().Equal(expectedMessageIds);
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task Given_SevenArchivedMessages_When_NavigatingForwardIsTrueAndSortByField_Then_ExpectedMessageAreReturned(
        FieldToSortBy sortedBy,
        DirectionToSortBy sortedDirection)
    {
        // Arrange
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)>()
            {
                new(
                    CreatedAt("2023-04-01T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345128",
                    "1234512345122",
                    DocumentType.NotifyAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-02T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345127",
                    "1234512345123",
                    DocumentType.NotifyWholesaleServices.Name),
                new(
                    CreatedAt("2023-04-03T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345125",
                    "1234512345121",
                    DocumentType.RejectRequestAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-04T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345126",
                    DocumentType.NotifyAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-05T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345128",
                    DocumentType.RejectRequestWholesaleSettlement.Name),
                new(
                    CreatedAt("2023-04-06T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345122",
                    "1234512345128",
                    DocumentType.NotifyAggregatedMeasureData.Name),
            };
        var recordIdsForMessages = new Dictionary<string, int>();
        var recordId = 0;
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            recordIdsForMessages.Add(messageToCreate.MessageId, recordId++);
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: messageToCreate.DocumentType,
                senderNumber: messageToCreate.Sender,
                receiverNumber: messageToCreate.Receiver);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: messages.Count,
            navigationForward: true,
            fieldToSortBy: sortedBy,
            directionSortBy: sortedDirection);

        // Act
        var result = await _sut.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);
        var orderedMessages = GetSortedMessaged(sortedBy, sortedDirection, messages, recordIdsForMessages);

        result.Messages.Select(x => x.MessageId)
            .Should()
            .Equal(orderedMessages.Select(x => x.MessageId));
    }

    [Theory]
    [MemberData(nameof(GetAllCombinationOfFieldsToSortByAndDirectionsToSortBy))]
    public async Task
        Given_SevenArchivedMessages_When_NavigatingBackwardIsTrueAndSortByField_Then_ExpectedMessageAreReturned(
            FieldToSortBy sortedBy,
            DirectionToSortBy sortedDirection)
    {
        // Arrange
        var messages =
            new List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)>()
            {
                new(
                    CreatedAt("2023-04-01T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345128",
                    "1234512345122",
                    DocumentType.NotifyAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-02T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345127",
                    "1234512345123",
                    DocumentType.NotifyWholesaleServices.Name),
                new(
                    CreatedAt("2023-04-03T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345125",
                    "1234512345121",
                    DocumentType.RejectRequestAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-04T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345126",
                    DocumentType.NotifyAggregatedMeasureData.Name),
                new(
                    CreatedAt("2023-04-05T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345123",
                    "1234512345128",
                    DocumentType.RejectRequestWholesaleSettlement.Name),
                new(
                    CreatedAt("2023-04-06T22:00:00Z"),
                    Guid.NewGuid().ToString(),
                    "1234512345122",
                    "1234512345128",
                    DocumentType.NotifyAggregatedMeasureData.Name),
            };
        var recordIdsForMessages = new Dictionary<string, int>();
        var recordId = 0;
        foreach (var messageToCreate in messages.OrderBy(_ => Random.Shared.Next()))
        {
            recordIdsForMessages.Add(messageToCreate.MessageId, recordId++);
            await _fixture.CreateArchivedMessageAsync(
                timestamp: messageToCreate.CreatedAt,
                messageId: messageToCreate.MessageId,
                documentType: messageToCreate.DocumentType,
                senderNumber: messageToCreate.Sender,
                receiverNumber: messageToCreate.Receiver);
        }

        var pagination = new SortedCursorBasedPagination(
            pageSize: messages.Count,
            navigationForward: false,
            fieldToSortBy: sortedBy,
            directionSortBy: sortedDirection);

        // Act
        var result = await _sut.SearchAsync(
                new GetMessagesQuery(pagination),
                CancellationToken.None);

        // Assert
        result.Messages.Should().HaveCount(messages.Count);
        var orderedMessages = GetSortedMessaged(sortedBy, sortedDirection, messages, recordIdsForMessages);

        result.Messages.Select(x => x.MessageId)
            .Should()
            .Equal(orderedMessages.Select(x => x.MessageId), $"Message is sorted by {sortedBy.Identifier} {sortedDirection.Identifier}");
    }

    #endregion

    private static Instant CreatedAt(string date)
    {
        return InstantPattern.General.Parse(date).Value;
    }

    private static
        IOrderedEnumerable<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)>
        GetSortedMessaged(
            FieldToSortBy sortedBy,
            DirectionToSortBy sortedDirection,
            List<(Instant CreatedAt, string MessageId, string Sender, string Receiver, string DocumentType)> messages,
            Dictionary<string, int> recordIdsForMessages)
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
                ? messages.OrderBy(x => x.CreatedAt).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortBy.DocumentType.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.DocumentType).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.DocumentType).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortBy.SenderNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.Sender).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.Sender).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        if (sortedBy.Identifier == FieldToSortBy.ReceiverNumber.Identifier)
        {
            orderedMessages = sortedDirection.Identifier == DirectionToSortBy.Ascending.Identifier
                ? messages.OrderBy(x => x.Receiver).ThenByDescending(x => recordIdsForMessages[x.MessageId])
                : messages.OrderByDescending(x => x.Receiver).ThenByDescending(x => recordIdsForMessages[x.MessageId]);
        }

        return orderedMessages;
    }

    private async Task<MessageSearchResult> SkipFirstPage(int pageSize, bool navigatingForward)
    {
        var pagination = new SortedCursorBasedPagination(pageSize: pageSize, navigationForward: navigatingForward);
        return await _sut.SearchAsync(
            new GetMessagesQuery(pagination),
            CancellationToken.None);
    }
}
