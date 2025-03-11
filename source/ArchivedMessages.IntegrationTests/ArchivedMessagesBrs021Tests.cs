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

/// <summary>
/// The sole purpose of this test is to verify that brs012 messages are stored in the database and not searchable.
/// It is currently being treated as a special case, as the messages are not supposed to be archived this way.
/// </summary>
[Collection(nameof(ArchivedMessagesCollection))]
public class ArchivedMessagesBrs021Tests : IAsyncLifetime
{
    private readonly IArchivedMessagesClient _sut;
    private readonly ArchivedMessagesFixture _fixture;

    private readonly ActorIdentity _authenticatedActor = new(
        actorNumber: ActorNumber.Create("1234512345811"),
        restriction: Restriction.None,
        actorRole: ActorRole.MeteredDataAdministrator,
        actorClientId: null,
        actorId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

    public ArchivedMessagesBrs021Tests(ArchivedMessagesFixture fixture, ITestOutputHelper testOutputHelper)
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
    public async Task Given_Brs021ArchivedMessages_When_Creating_Then_MessageIsStoredInDatabaseAndNotSearchable()
    {
        // Arrange
        var incomingMessage = await _fixture.CreateArchivedMessageAsync(
            archivedMessageType: ArchivedMessageTypeDto.IncomingMessage,
            documentType: IncomingDocumentType.NotifyValidatedMeasureData.Name,
            storeMessage: false);

        var outgoingMessage = await _fixture.CreateArchivedMessageAsync(
            archivedMessageType: ArchivedMessageTypeDto.OutgoingMessage,
            documentType: DocumentType.NotifyValidatedMeasureData.Name,
            storeMessage: false);

        // Act
        await _sut.CreateAsync(incomingMessage, CancellationToken.None);
        await _sut.CreateAsync(outgoingMessage, CancellationToken.None);

        // Assert
        var dbResult = await _fixture.GetAllMessagesInDatabase();
        var searchResult = await _sut.SearchAsync(
            new GetMessagesQueryDto(
                new SortedCursorBasedPaginationDto()),
            CancellationToken.None);

        using var assertionScope = new AssertionScope();
        dbResult.Should().NotBeNull().And.HaveCount(2);
        searchResult.Should().NotBeNull();
        searchResult.Messages.Should().BeEmpty();
        searchResult.TotalAmountOfMessages.Should().Be(0);
    }
}
