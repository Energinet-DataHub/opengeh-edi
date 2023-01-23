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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.Transactions.Aggregations;

public class AggregationResultForwarding : Entity
{
#pragma warning disable
    private readonly List<OutgoingMessage> _messages = new();

    private readonly ActorNumber _receivingActor;

    private readonly MarketRole _receivingActorRole;

    private readonly ProcessType _processType;
    public AggregationResultForwarding(TransactionId id, ActorNumber receivingActor, MarketRole receivingActorRole, ProcessType processType)
    {
        _receivingActor = receivingActor;
        _receivingActorRole = receivingActorRole;
        _processType = processType;
        Id = id;
    }

    public void SendResult(AggregationResult aggregationResult)
    {
        _messages.Add(AggregationResultMessage.Create(_receivingActor, _receivingActorRole, Id, _processType, aggregationResult));
    }

    #pragma warning disable CS8618 // EF core need this private constructor
    private AggregationResultForwarding()
    {
    }
    #pragma warning restore

    public TransactionId Id { get; }
}
