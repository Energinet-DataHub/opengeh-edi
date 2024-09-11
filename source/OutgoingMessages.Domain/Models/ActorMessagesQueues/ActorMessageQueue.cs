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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;

public class ActorMessageQueue
{
    /// <summary>
    /// Create new actor message queue for the given <paramref name="receiver"/>
    /// </summary>
    private ActorMessageQueue(Receiver receiver)
    {
        Receiver = receiver;
        Id = ActorMessageQueueId.New();
    }

    public ActorMessageQueueId Id { get; private set; }

    public Receiver Receiver { get; set; }

    #pragma warning disable
    private ActorMessageQueue()
    {
    }

    public static ActorMessageQueue CreateFor(Receiver receiver)
    {
        return new ActorMessageQueue(receiver);
    }
}
