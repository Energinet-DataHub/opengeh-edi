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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Usecases;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions;

internal sealed class OutgoingMessageExceptionSimulator : OutgoingMessagesClient
{
    public OutgoingMessageExceptionSimulator(
        Peek peek,
        Dequeue dequeue,
        Enqueue enqueue,
        ActorMessageQueueContext actorMessageQueueContext,
        ISystemDateTimeProvider systemDateTimeProvider,
        ISerializer serializer)
        : base(
            peek,
            dequeue,
            enqueue,
            actorMessageQueueContext,
            systemDateTimeProvider,
            serializer)
    {
    }

    public override Task EnqueueAndCommitAsync(WholesaleServicesMessageDto wholesaleServicesMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wholesaleServicesMessage);
        if (wholesaleServicesMessage.ReceiverRole == ActorRole.EnergySupplier)
        {
            throw new InvalidDataException("Simulated exception.");
        }

        return base.EnqueueAndCommitAsync(wholesaleServicesMessage, cancellationToken);
    }
}
