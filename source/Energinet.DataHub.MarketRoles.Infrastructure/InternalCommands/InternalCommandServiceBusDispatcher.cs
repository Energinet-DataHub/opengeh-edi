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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class InternalCommandServiceBusDispatcher : IInternalCommandDispatcher
    {
        private readonly ServiceBusSender _serviceBusSender;

        public InternalCommandServiceBusDispatcher(ServiceBusSender serviceBusSender)
        {
            _serviceBusSender = serviceBusSender ?? throw new ArgumentNullException(nameof(serviceBusSender));
        }

        public async Task<DispatchResult> DispatchAsync(QueuedInternalCommand queuedInternalCommand)
        {
            if (queuedInternalCommand == null) throw new ArgumentNullException(nameof(queuedInternalCommand));
            var message = CreateMessageFrom(queuedInternalCommand);

            if (queuedInternalCommand.ScheduleDate.HasValue)
            {
                var sequenceNumber = await _serviceBusSender.ScheduleMessageAsync(message, queuedInternalCommand.ScheduleDate.Value.ToDateTimeOffset()).ConfigureAwait(false);
                return DispatchResult.Create(sequenceNumber);
            }

            await _serviceBusSender.SendMessageAsync(message).ConfigureAwait(false);
            return DispatchResult.Create(default);
        }

        private static ServiceBusMessage CreateMessageFrom(QueuedInternalCommand queuedInternalCommand)
        {
            var message = new ServiceBusMessage(queuedInternalCommand.Data);
            message.ContentType = "application/octet-stream";
            message.MessageId = queuedInternalCommand.Id.ToString();
            message.CorrelationId = queuedInternalCommand.Correlation;

            return message;
        }
    }
}
