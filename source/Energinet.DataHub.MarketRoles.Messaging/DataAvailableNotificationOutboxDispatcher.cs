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
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;

namespace Energinet.DataHub.MarketRoles.Messaging
{
    public class DataAvailableNotificationOutboxDispatcher : IOutboxDispatcher<DataAvailableNotification>
    {
        private readonly IOutboxMessageFactory _outboxMessageFactory;
        private readonly IOutbox _outbox;

        public DataAvailableNotificationOutboxDispatcher(IOutboxMessageFactory outboxMessageFactory, IOutbox outbox)
        {
            _outboxMessageFactory = outboxMessageFactory;
            _outbox = outbox;
        }

        public void Dispatch(DataAvailableNotification message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            var outboxMessage = _outboxMessageFactory.CreateFrom(message, OutboxMessageCategory.MessageHub);
            _outbox.Add(outboxMessage);
        }
    }
}
