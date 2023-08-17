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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Application.Configuration.TimeEvents;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.Configuration.IntegrationEvents;
using IntegrationTests.Fixtures;
using NodaTime;
using Xunit;

namespace IntegrationTests.Infrastructure.Retention;

public class RemoveMonthOldReceivedIntegrationEventIdsWhenADaysHasPassedTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly RemoveMonthOldReceivedIntegrationEventIdsWhenADaysHasPassed _sut;

    public RemoveMonthOldReceivedIntegrationEventIdsWhenADaysHasPassedTests(
        DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _sut = new RemoveMonthOldReceivedIntegrationEventIdsWhenADaysHasPassed(
            GetService<IDatabaseConnectionFactory>());
    }

    [Fact]
    public async Task Clean_up_received_integration_event_ids_older_then_a_month()
    {
        // arrange
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(31));
        var amountOfOldIntegrationEventIds = 25000;
        var amountOfNotOldIntegrationEventsIds = 25;
        await GenerateReceivedIntegrationEvents(amountOfOldIntegrationEventIds, amountOfNotOldIntegrationEventsIds, monthAgo);

        // Act
        await _sut.Handle(new ADayHasPassed(_systemDateTimeProvider.Now()), CancellationToken.None);

        // Assert
        AssertReceivedIntegrationEventIdsIsRemoved(amountOfNotOldIntegrationEventsIds, monthAgo);
    }

    protected override void Dispose(bool disposing)
    {
        _b2BContext.Dispose();
        base.Dispose(disposing);
    }

    private void AssertReceivedIntegrationEventIdsIsRemoved(int amountOfNotOldIntegrationEvents, Instant monthAgo)
    {
        var oldIntegrationEventIds = _b2BContext.ReceivedIntegrationEventIds
            .Where(command => command.OccurredOn < monthAgo);
        var notOldIntegrationEventIds = _b2BContext.ReceivedIntegrationEventIds
            .Where(command => command.OccurredOn >= monthAgo);

        Assert.Equal(amountOfNotOldIntegrationEvents, notOldIntegrationEventIds.Count());
        Assert.Empty(oldIntegrationEventIds);
    }

    private async Task GenerateReceivedIntegrationEvents(
        int amountOfOldIntegrationEventIds,
        int amountOfNotOldIntegrationEventIds,
        Instant monthAgo)
    {
        for (var i = 0; i < amountOfOldIntegrationEventIds; i++)
        {
            var processedCommand = new ReceivedIntegrationEventId(Guid.NewGuid().ToString(), monthAgo);
            _b2BContext.ReceivedIntegrationEventIds.Add(processedCommand);
        }

        for (var i = 0; i < amountOfNotOldIntegrationEventIds; i++)
        {
            var processedCommand = new ReceivedIntegrationEventId(
                Guid.NewGuid().ToString(),
                _systemDateTimeProvider.Now());
            _b2BContext.ReceivedIntegrationEventIds.Add(processedCommand);
        }

        await _b2BContext.SaveChangesAsync(CancellationToken.None);
    }
}
