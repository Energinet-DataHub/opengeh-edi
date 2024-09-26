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

using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Fixture;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Tests;

[Collection(nameof(IntegrationEventsIntegrationTestCollectionFixture))]
public class WhenActorCertificateCredentialsRemovedEventIsReceived : IntegrationEventsTestBase
{
    public WhenActorCertificateCredentialsRemovedEventIsReceived(IntegrationEventsFixture integrationEventsFixture, ITestOutputHelper testOutputHelper)
        : base(integrationEventsFixture, testOutputHelper)
    {
        SetupServiceCollection();
    }

    [Fact]
    public async Task When_received_ActorCertificateCredentialsRemoved_event_for_existing_ActorCertificate_then_ActorCertificate_is_removed()
    {
        // Arrange
        var actorNumberToBeRemoved = "1234567891234568";
        var certificateThumbprintToBeRemoved = "12346";
        await CreateCertifiedActorAsync(actorNumberToBeRemoved, certificateThumbprintToBeRemoved);

        var actorCertificateCredentialsRemovedEvent = new ActorCertificateCredentialsRemovedEventBuilder()
            .SetActorNumber(actorNumberToBeRemoved)
            .SetCertificateThumbprint(certificateThumbprintToBeRemoved)
            .Build();

        // Act
        await HavingReceivedAndHandledIntegrationEventAsync(actorCertificateCredentialsRemovedEvent);

        // Assert
        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Empty(actorCertificates);
    }

    [Fact]
    public async Task When_received_multiple_ActorCertificateCredentialsRemoved_events_for_a_single_existing_ActorCertificate_is_removed_runs_successfully()
    {
        // Arrange
        var actorNumberToBeRemoved = "1234567891234568";
        var certificateThumbprintToBeRemoved = "12346";
        await CreateCertifiedActorAsync(actorNumberToBeRemoved, certificateThumbprintToBeRemoved);

        var integrationEvent1 = new ActorCertificateCredentialsRemovedEventBuilder()
            .SetActorNumber(actorNumberToBeRemoved)
            .SetCertificateThumbprint(certificateThumbprintToBeRemoved)
            .Build();

        var integrationEvent2 = new ActorCertificateCredentialsRemovedEventBuilder()
            .Build();

        // Act
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent1);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent2);

        // Assert
        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Empty(actorCertificates);
    }

    [Fact]
    public async Task When_received_ActorCertificateCredentialsRemoved_event_for_not_existing_ActorCertificate_runs_successfully()
    {
        // Arrange
        var existingActorNumber = "1234567891234567";
        var existingCertificateThumbprint = "12345";
        var actorNumberToBeRemoved = "1234567891234568";
        var certificateThumbprintToBeRemoved = "12346";
        await CreateCertifiedActorAsync(existingActorNumber, existingCertificateThumbprint);

        var integrationEvent = new ActorCertificateCredentialsRemovedEventBuilder()
            .SetActorNumber(actorNumberToBeRemoved)
            .SetCertificateThumbprint(certificateThumbprintToBeRemoved)
            .Build();

        // Act
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent);

        // Assert
        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Single(actorCertificates);

        Assert.Multiple(
            () => Assert.Equal(existingActorNumber, actorCertificates.Single().ActorNumber),
            () => Assert.Equal(ActorRole.MeteredDataResponsible.Code, actorCertificates.Single().ActorRole),
            () => Assert.Equal(existingCertificateThumbprint, actorCertificates.Single().Thumbprint));
    }

    private async Task CreateCertifiedActorAsync(string actorNumber, string thumbprint)
    {
        var validFrom = Instant.FromUtc(2022, 6, 6, 0, 0);
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory!.GetConnectionAndOpenAsync(CancellationToken.None);
        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[ActorCertificate] ([Id], [ActorNumber], [ActorRole], [Thumbprint], [ValidFrom], [SequenceNumber])
                    VALUES (@id, @actorNumber, @actorRole, @thumbprint, @validFrom, @sequenceNumber)",
            new { id = Guid.NewGuid(), actorNumber = actorNumber, actorRole = "MDR", thumbprint = thumbprint, validFrom = validFrom, sequenceNumber = 1 });
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(ActorCertificateCredentialsRemoved actorCertificateCredentialsRemoved)
    {
        var integrationEventHandler = Services.GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), ActorCertificateCredentialsRemoved.EventName, 1, actorCertificateCredentialsRemoved);

        await integrationEventHandler!.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<List<ActorCertificateForTest>> GetActorCertificatesFromDatabaseAsync()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory!.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT ActorNumber, ActorRole, Thumbprint, ValidFrom, SequenceNumber FROM [dbo].[ActorCertificate]";
        var results = await connection.QueryAsync<ActorCertificateForTest>(sql);
        return results.ToList();
    }

    internal sealed record ActorCertificateForTest(string ActorNumber, string ActorRole, string Thumbprint, Instant ValidFrom, int SequenceNumber);
}
