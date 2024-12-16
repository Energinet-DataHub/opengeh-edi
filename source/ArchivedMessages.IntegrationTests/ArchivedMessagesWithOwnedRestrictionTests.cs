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
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests;

[Collection(nameof(ArchivedMessagesCollection))]
public class ArchivedMessagesWithOwnedRestrictionTests : IAsyncLifetime
{
    private static readonly Guid _actorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    private readonly ActorIdentity _authenticatedActor = new(
        ActorNumber.Create("1234512345888"),
        Restriction.Owned,
        ActorRole.EnergySupplier,
        _actorId);

    public ArchivedMessagesWithOwnedRestrictionTests(ArchivedMessagesFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;

        var services = _fixture.BuildService(testOutputHelper);

        services.GetRequiredService<AuthenticatedActor>().SetAuthenticatedActor(_authenticatedActor);
        _sut = services.GetRequiredService<IArchivedMessagesClient>();
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
    public async Task Given_ArchivedMessagesInStorage_When_GettingMessageToAnotherActor_Then_ReturnsNoMessage()
    {
        // Arrange
        var notAuthActor = "9999999999999";
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            receiverNumber: notAuthActor,
            senderNumber: notAuthActor);

        // Act
        var result = await _sut.GetAsync(archivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_ArchivedMessages_When_GettingMessageWithAuthActorAsReceiver_Then_ReturnsOwnMessage()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);

        // Act
        var result = await _sut.GetAsync(archivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ArchivedMessages_When_GettingMessageWithAuthActorAsSender_Then_ReturnsOwnMessage()
    {
        // Arrange
        var archivedMessage = await _fixture.CreateArchivedMessageAsync(
            senderNumber: _authenticatedActor.ActorNumber.Value,
            senderRole: _authenticatedActor.ActorRole);

        // Act
        var result = await _sut.GetAsync(archivedMessage.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Stream.Should().NotBeNull();
    }

    [Fact]
    public async Task Given_ThreeArchivedMessages_When_SearchingWithoutCriteria_Then_ReturnsOwnMessage()
    {
        // Arrange
        await _fixture.CreateArchivedMessageAsync(receiverNumber: "9999999999999");
        await _fixture.CreateArchivedMessageAsync(
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);
        await _fixture.CreateArchivedMessageAsync(
            senderNumber: _authenticatedActor.ActorNumber.Value,
            senderRole: _authenticatedActor.ActorRole);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(new SortedCursorBasedPaginationDto()),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should()
            .HaveCount(2)
            .And.OnlyContain(
                message => message.ReceiverNumber == _authenticatedActor.ActorNumber.Value
                           || message.SenderNumber == _authenticatedActor.ActorNumber.Value);
    }

    #region include_related_messages

    [Fact]
    public async Task Given_TwoArchivedMessagesWithRelation_When_ExcludingRelatedMessagesAndSearchingByMessageId_Then_RelatedMessagesAreNotIncluded()
    {
        // Arrange
        var expectedMessageId = Guid.NewGuid().ToString();
        await _fixture.CreateArchivedMessageAsync(
            messageId: expectedMessageId,
            archivedMessageType: ArchivedMessageTypeDto.IncomingMessage,
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);
        await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(expectedMessageId),
            archivedMessageType: ArchivedMessageTypeDto.OutgoingMessage,
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);

        // Act
        var result = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MessageId: expectedMessageId,
                IncludeRelatedMessages: false),
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        using var assertionScope = new AssertionScope();
        result.Messages.Should()
            .ContainSingle()
            .Which.MessageId.Should()
            .Be(expectedMessageId);
    }

    [Fact]
    public async Task Given_FourArchivedMessagesWithRelations_When_IncludingRelatedMessagesAndSearchingByMessageId_Then_RelatedMessagesAreReturned()
    {
        // Arrange
        var messageWithoutRelation = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: null,
            archivedMessageType: ArchivedMessageTypeDto.IncomingMessage,
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);
        var messageWithRelation = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(messageWithoutRelation.MessageId!),
            archivedMessageType: ArchivedMessageTypeDto.OutgoingMessage,
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);
        var messageWithRelation2 = await _fixture.CreateArchivedMessageAsync(
            relatedToMessageId: MessageId.Create(messageWithoutRelation.MessageId!),
            archivedMessageType: ArchivedMessageTypeDto.OutgoingMessage,
            receiverNumber: _authenticatedActor.ActorNumber.Value,
            receiverRole: _authenticatedActor.ActorRole);
        var unexpectedMessage = await _fixture.CreateArchivedMessageAsync();

        // Act
        // This could simulate a search for a message, where the message is a request with two responses
        var searchForRequest = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
                MessageId: messageWithoutRelation.MessageId,
                IncludeRelatedMessages: true),
            CancellationToken.None);

        // This could simulate a search for a message, where the message is a response to a request with two responses
        var searchForResponse = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto(),
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
}
