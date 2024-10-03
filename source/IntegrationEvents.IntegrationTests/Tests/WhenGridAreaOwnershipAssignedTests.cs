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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Builders;
using Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Fixture;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Tests;

[Collection(nameof(IntegrationEventsIntegrationTestCollection))]
public class WhenGridAreaOwnershipAssignedTests : IntegrationEventsTestBase
{
    private readonly GridAreaOwnershipAssignedEventBuilder _gridAreaOwnershipAssignedEventBuilder = new();

    public WhenGridAreaOwnershipAssignedTests(IntegrationEventsFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        SetupServiceCollection();
    }

    [Fact]
    public async Task Given_GridAreaOwnershipAssigned_When_IsReceived_Then_StoresGridAreaOwner()
    {
        var gridAreaOwnershipAssignedEvent = _gridAreaOwnershipAssignedEventBuilder.Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent);

        var gridAreas = await GetGridAreas();
        Assert.Single(gridAreas);
        var gridArea = gridAreas.Single();
        Assert.Equal(gridArea.GridAreaCode, gridAreaOwnershipAssignedEvent.GridAreaCode);
        Assert.Equal(gridArea.GridAreaOwnerActorNumber, gridAreaOwnershipAssignedEvent.ActorNumber);
        Assert.Equal(gridArea.SequenceNumber, gridAreaOwnershipAssignedEvent.SequenceNumber);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, GridAreaOwnershipAssigned gridAreaOwnershipAssigned)
    {
        var integrationEventHandler = Services.GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, gridAreaOwnershipAssigned);

        await integrationEventHandler!.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<IEnumerable<GridArea>> GetGridAreas()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory!.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT GridAreaCode, GridAreaOwnerActorNumber, SequenceNumber FROM [dbo].[GridAreaOwner]";
        return await connection.QueryAsync<GridArea>(sql);
    }

#pragma warning disable
    public record GridArea(string GridAreaCode, string GridAreaOwnerActorNumber, int SequenceNumber);
#pragma warning restore
}
