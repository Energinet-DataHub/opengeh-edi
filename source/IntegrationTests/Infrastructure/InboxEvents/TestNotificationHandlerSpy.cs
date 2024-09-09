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

using MediatR;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class TestNotificationHandlerSpy : INotificationHandler<TestNotification>
{
    private static readonly List<TestNotification> _testNotifications = new();
    private static readonly List<string> _notifications = new();

    public static void AddNotification(string notification)
    {
        _notifications.Add(notification);
    }

    public static void AssertExpectedNotifications()
    {
        Assert.NotNull(_testNotifications);
        Assert.Contains(
            _notifications,
            notificationString => _testNotifications.Any(
                testNotification => testNotification.AProperty == notificationString));
    }

    public Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        _testNotifications.Add(notification);
        return Task.CompletedTask;
    }
}
