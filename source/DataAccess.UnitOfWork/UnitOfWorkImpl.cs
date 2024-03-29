﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.DataAccess.UnitOfWork;

#pragma warning disable CA1711
public sealed class UnitOfWorkImpl : IUnitOfWork
#pragma warning restore CA1711
{
    private readonly ProcessContext _processContext;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly IncomingMessagesContext _incomingMessagesContext;

    public UnitOfWorkImpl(
        ProcessContext processContext,
        ActorMessageQueueContext actorMessageQueueContext,
        IncomingMessagesContext incomingMessagesContext)
    {
        _processContext = processContext;
        _actorMessageQueueContext = actorMessageQueueContext;
        _incomingMessagesContext = incomingMessagesContext;
    }

    public async Task CommitTransactionAsync()
    {
        await ResilientTransaction.New(_processContext)
            .SaveChangesAsync(new DbContext[] { _processContext, _actorMessageQueueContext, _incomingMessagesContext })
            .ConfigureAwait(false);
    }
}
