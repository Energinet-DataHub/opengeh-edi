﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.ActorCertificate;

[SuppressMessage("Style", "VSTHRD200", Justification = "Test class")]
[SuppressMessage("Naming", "CA1707", Justification = "Test class")]
[IntegrationTest]
public class WhenActorCertificateCredentialsAssignedEventIsReceived : TestBase
{
    public WhenActorCertificateCredentialsAssignedEventIsReceived(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task When_received_ActorCertificateCredentialsAssigned_event_and_no_ActorCertificate_exists_creates_new_ActorCertificate_with_correct_values()
    {
        // Arrange
        var integrationEvent = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("1234567891234567")
            .SetActorRole(EicFunction.EnergySupplier)
            .SetCertificateThumbprint("12345")
            .SetValidFrom(Instant.FromUtc(2023, 12, 1, 0, 0))
            .SetSequenceNumber(1)
            .Build();

        // Act
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent);

        // Assert
        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Single(actorCertificates);
        Assert.Multiple(
            () => Assert.Equal("1234567891234567", actorCertificates.Single().ActorNumber),
            () => Assert.Equal(MarketRole.EnergySupplier.Code, actorCertificates.Single().ActorRole),
            () => Assert.Equal("12345", actorCertificates.Single().Thumbprint),
            () => Assert.Equal(Instant.FromUtc(2023, 12, 1, 0, 0), actorCertificates.Single().ValidFrom),
            () => Assert.Equal(1, actorCertificates.Single().SequenceNumber));
    }

    [Fact]
    public async Task When_received_ActorCertificateCredentialsAssigned_event_and_ActorCertificate_already_exists_then_ActorCertificate_is_updated_with_correct_values()
    {
        // Arrange
        var integrationEvent1 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetCertificateThumbprint("123")
            .SetValidFrom(Instant.FromUtc(2020, 1, 1, 0, 0))
            .SetSequenceNumber(1)
            .Build();

        var integrationEvent2 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetCertificateThumbprint("abc")
            .SetValidFrom(Instant.FromUtc(2022, 6, 6, 0, 0))
            .SetSequenceNumber(2)
            .Build();

        // Act
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent1);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent2);

        // Assert
        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Single(actorCertificates);
        Assert.Multiple(
            () => Assert.Equal("abc", actorCertificates.Single().Thumbprint),
            () => Assert.Equal(Instant.FromUtc(2022, 6, 6, 0, 0), actorCertificates.Single().ValidFrom),
            () => Assert.Equal(2, actorCertificates.Single().SequenceNumber));
    }

    [Fact]
    public async Task When_received_multiple_ActorCertificateCredentialsAssigned_events_for_different_actors_then_correct_count_exists()
    {
        var integrationEventForActorRole1 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("1111111111111")
            .SetActorRole(EicFunction.EnergySupplier)
            .SetSequenceNumber(1)
            .Build();

        var integrationEventForActorRole2 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("1111111111111")
            .SetActorRole(EicFunction.BalanceResponsibleParty)
            .SetSequenceNumber(1)
            .Build();

        var integrationEventForActorRole2b = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("1111111111111")
            .SetActorRole(EicFunction.BalanceResponsibleParty)
            .SetSequenceNumber(2)
            .Build();

        var integrationEventForActorRole3 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("2222222222222")
            .SetActorRole(EicFunction.EnergySupplier)
            .SetSequenceNumber(1)
            .Build();

        var integrationEventForActorRole4 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetActorNumber("2222222222222")
            .SetActorRole(EicFunction.BalanceResponsibleParty)
            .SetSequenceNumber(1)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(integrationEventForActorRole1);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEventForActorRole2);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEventForActorRole2b);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEventForActorRole3);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEventForActorRole4);

        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Equal(4, actorCertificates.Count);
    }

    [Fact]
    public async Task When_received_multiple_ActorCertificateCredentialsAssigned_events_out_of_order_then_highest_sequence_number_is_applied_with_correct_values()
    {
        var integrationEvent1 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetSequenceNumber(1)
            .SetCertificateThumbprint("thumbprint-a")
            .Build();

        var integrationEvent4 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetSequenceNumber(4)
            .SetCertificateThumbprint("thumbprint-b")
            .Build();

        var integrationEvent2 = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetSequenceNumber(2)
            .SetCertificateThumbprint("thumbprint-a")
            .Build();

        var integrationEvent1b = new ActorCertificateCredentialsAssignedEventBuilder()
            .SetSequenceNumber(1)
            .SetCertificateThumbprint("thumbprint-a")
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent1);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent4);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent2);
        await HavingReceivedAndHandledIntegrationEventAsync(integrationEvent1b);

        var actorCertificates = await GetActorCertificatesFromDatabaseAsync();

        Assert.Single(actorCertificates);
        Assert.Multiple(
            () => Assert.Equal("thumbprint-b", actorCertificates.Single().Thumbprint),
            () => Assert.Equal(4, actorCertificates.Single().SequenceNumber));
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(ActorCertificateCredentialsAssigned actorCertificateCredentialsAssigned)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), ActorCertificateCredentialsAssigned.EventName, 1, actorCertificateCredentialsAssigned);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<List<ActorCertificateForTest>> GetActorCertificatesFromDatabaseAsync()
    {
        var connectionFactory = GetService<IDatabaseConnectionFactory>();

        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT ActorNumber, ActorRole, Thumbprint, ValidFrom, SequenceNumber FROM [dbo].[ActorCertificate]";
        var results = await connection.QueryAsync<ActorCertificateForTest>(sql);
        return results.ToList();
    }

    internal sealed record ActorCertificateForTest(string ActorNumber, string ActorRole, string Thumbprint, Instant ValidFrom, int SequenceNumber);
}
