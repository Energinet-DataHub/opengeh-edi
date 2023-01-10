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

using System.Threading.Tasks;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Infrastructure.OutgoingMessages.Peek;

namespace Messaging.IntegrationTests.TestDoubles;

public class ReadyMessagesStub : ReadyMessages
{
    private bool _shouldReturnEmptyMessage;

    public ReadyMessagesStub(IDatabaseConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public override Task<ReadyMessage?> GetAsync(MessageCategory category, ActorNumber receiverNumber)
    {
        if (_shouldReturnEmptyMessage)
        {
            _shouldReturnEmptyMessage = false;
            return Task.FromResult(default(ReadyMessage));
        }

        return base.GetAsync(category, receiverNumber);
    }

    public void ReturnsEmptyMessage()
    {
        _shouldReturnEmptyMessage = true;
    }
}
