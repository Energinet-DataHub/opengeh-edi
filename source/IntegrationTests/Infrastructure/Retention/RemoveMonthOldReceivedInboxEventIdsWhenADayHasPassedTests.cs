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
using Infrastructure.InboxEvents;
using IntegrationTests.Fixtures;
using NodaTime;
using Xunit;

namespace IntegrationTests.Infrastructure.Retention;

public class RemoveMonthOldReceivedInboxEventIdsWhenADayHasPassedTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly RemoveMonthOldReceivedInboxEventIdsWhenADayHasPassed _sut;

    public RemoveMonthOldReceivedInboxEventIdsWhenADayHasPassedTests(
        DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _sut = new RemoveMonthOldReceivedInboxEventIdsWhenADayHasPassed(
            GetService<IDatabaseConnectionFactory>());
    }

    [Fact]
    public async Task Clean_up_received_inbox_event_ids_older_then_a_month()
    {
        // arrange
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(31));
        var amountOfOldInboxEventIds = 25000;
        var amountOfNotOldInboxEventsIds = 25;
        await GenerateReceivedInboxEvents(amountOfOldInboxEventIds, amountOfNotOldInboxEventsIds, monthAgo);

        // Act
        await _sut.Handle(new ADayHasPassed(_systemDateTimeProvider.Now()), CancellationToken.None);

        // Assert
        AssertReceivedInboxEventIdsIsRemoved(amountOfNotOldInboxEventsIds, monthAgo);
    }

    protected override void Dispose(bool disposing)
    {
        _b2BContext.Dispose();
        base.Dispose(disposing);
    }

    private void AssertReceivedInboxEventIdsIsRemoved(int amountOfNotOldInboxEvents, Instant monthAgo)
    {
        var oldInboxEventIds = _b2BContext.ReceivedInboxEventIds
            .Where(command => command.OccurredOn < monthAgo);
        var notOldInboxEventIds = _b2BContext.ReceivedInboxEventIds
            .Where(command => command.OccurredOn >= monthAgo);

        Assert.Equal(amountOfNotOldInboxEvents, notOldInboxEventIds.Count());
        Assert.Empty(oldInboxEventIds);
    }

    private async Task GenerateReceivedInboxEvents(
        int amountOfOldInboxEventIds,
        int amountOfNotOldInboxEventIds,
        Instant monthAgo)
    {
        for (var i = 0; i < amountOfOldInboxEventIds; i++)
        {
            var processedCommand = new ReceivedInboxEventId(Guid.NewGuid().ToString(), monthAgo);
            _b2BContext.ReceivedInboxEventIds.Add(processedCommand);
        }

        for (var i = 0; i < amountOfNotOldInboxEventIds; i++)
        {
            var processedCommand = new ReceivedInboxEventId(
                Guid.NewGuid().ToString(),
                _systemDateTimeProvider.Now());
            _b2BContext.ReceivedInboxEventIds.Add(processedCommand);
        }

        await _b2BContext.SaveChangesAsync(CancellationToken.None);
    }
}
