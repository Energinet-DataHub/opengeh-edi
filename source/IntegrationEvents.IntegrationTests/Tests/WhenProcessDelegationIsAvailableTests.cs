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
public class WhenProcessDelegationIsAvailableTests : IntegrationEventsTestBase
{
    public WhenProcessDelegationIsAvailableTests(IntegrationEventsFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        SetupServiceCollection();
    }

    [Fact]
    public async Task Given_ProcessDelegationEvent_When_IsReceived_Then_StoresDelegation()
    {
        var processDelegationEvent = ProcessDelegationEventBuilder.Build();

        await HavingReceivedAndHandledIntegrationEventAsync(ProcessDelegationConfigured.EventName, processDelegationEvent);

        var processDelegation = await GetProcessDelegationIds(processDelegationEvent.DelegatedToActorNumber);

        Assert.Single(processDelegation);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, ProcessDelegationConfigured processDelegationConfigured)
    {
        var integrationEventHandler = Services.GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, processDelegationConfigured);

        await integrationEventHandler!.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<IEnumerable<Guid>> GetProcessDelegationIds(string delegatedToActorNumber)
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory!.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $@"SELECT [Id] FROM [dbo].[ProcessDelegation] WHERE DelegatedToActorNumber = '{delegatedToActorNumber}'";
        return await connection.QueryAsync<Guid>(sql);
    }
}
