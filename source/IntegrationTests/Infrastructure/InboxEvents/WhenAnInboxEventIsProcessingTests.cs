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

using System.Text;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class WhenAnInboxEventIsProcessingTests : TestBase
{
    private readonly string _eventType = nameof(TestInboxEvent);
    private readonly Guid _referenceId = Guid.NewGuid();
    private readonly string _eventId = Guid.NewGuid().ToString();
    private readonly InboxEventsProcessor _inboxProcessor;
    private readonly TestInboxEventMapper _testInboxEventMapper;

    public WhenAnInboxEventIsProcessingTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _testInboxEventMapper = new TestInboxEventMapper();
        _inboxProcessor = new InboxEventsProcessor(
            GetService<IDatabaseConnectionFactory>(),
            GetService<IMediator>(),
            GetService<ISystemDateTimeProvider>(),
            new[] { _testInboxEventMapper },
            GetService<ILogger<InboxEventsProcessor>>());
    }

    [Fact]
    public async Task Event_is_marked_as_processed_when_a_handler_has_handled_it_successfully()
    {
        await RegisterInboxEvent();
        await ProcessInboxMessages();

        await EventIsMarkedAsProcessed(_eventId);
    }

    [Fact]
    public async Task Notification_for_events_is_published()
    {
        await RegisterInboxEvent();
        TestNotificationHandlerSpy.AddNotification("Event1");

        await ProcessInboxMessages();

        TestNotificationHandlerSpy.AssertExpectedNotifications();
    }

    [Fact]
    public async Task Event_is_marked_as_failed_if_the_event_mapper_is_missing_for_the_eventType()
    {
        await RegisterInboxEvent();
        var inboxProcessor = new InboxEventsProcessor(
            GetService<IDatabaseConnectionFactory>(),
            GetService<IMediator>(),
            GetService<ISystemDateTimeProvider>(),
            Array.Empty<IInboxEventMapper>(),
            GetService<ILogger<InboxEventsProcessor>>());

        await inboxProcessor.ProcessEventsAsync(CancellationToken.None);

        await EventIsMarkedAsFailed(_eventId);
    }

    private Task ProcessInboxMessages()
    {
        return _inboxProcessor.ProcessEventsAsync(CancellationToken.None);
    }

    private async Task EventIsMarkedAsProcessed(string eventId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var isProcessed = await connection.ExecuteScalarAsync<bool>($"SELECT COUNT(*) FROM dbo.ReceivedInboxEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL", new { EventId = eventId, });
        Assert.True(isProcessed);
    }

    private async Task EventIsMarkedAsFailed(string eventId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var isFailed = await connection.ExecuteScalarAsync<bool>($"SELECT COUNT(*) FROM dbo.ReceivedInboxEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL AND ErrorMessage IS NOT NULL", new { EventId = eventId, });
        Assert.True(isFailed);
    }

    private async Task RegisterInboxEvent()
    {
        var context = GetService<ProcessContext>();
        context.ReceivedInboxEvents.Add(new ReceivedInboxEvent(
            _eventId,
            _eventType,
            _referenceId,
            Encoding.ASCII.GetBytes("Event1"),
            GetService<ISystemDateTimeProvider>().Now()));
        await context.SaveChangesAsync();
    }
}
