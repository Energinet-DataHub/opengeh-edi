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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using IncomingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly B2BContext _b2BContext;
        private readonly ProcessContext _processContext;
        private readonly ActorMessageQueueContext _actorMessageQueueContext;
        private readonly IncomingMessagesContext _incomingMessagesContext;

        public UnitOfWork(
            B2BContext b2BContext,
            ProcessContext processContext,
            ActorMessageQueueContext actorMessageQueueContext,
            IncomingMessagesContext incomingMessagesContext)
        {
            _b2BContext = b2BContext;
            _processContext = processContext;
            _actorMessageQueueContext = actorMessageQueueContext;
            _incomingMessagesContext = incomingMessagesContext;
        }

        public async Task CommitTransactionAsync()
        {
            // Use of an EF Core resiliency strategy when using multiple DbContexts
            // within an explicit BeginTransaction():
            // https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
            await ResilientTransaction.New(_processContext).SaveChangesAsync(new DbContext[]
            {
                _b2BContext, _processContext, _actorMessageQueueContext, _incomingMessagesContext,
            }).ConfigureAwait(false);
        }
    }
}
