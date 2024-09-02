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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.DataAccess.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IReadOnlyCollection<DbContext> _contexts;
    // private readonly ProcessContext _processContext;
    // private readonly ActorMessageQueueContext _actorMessageQueueContext;
    // private readonly IncomingMessagesContext _incomingMessagesContext;

    public UnitOfWork(
        IEnumerable<DbContext> dbContexts)
    {
        _contexts = dbContexts.ToArray();
    }

    public async Task CommitTransactionAsync()
    {
        await ResilientTransaction.New()
            .SaveChangesAsync(_contexts)
            .ConfigureAwait(false);
    }
}
