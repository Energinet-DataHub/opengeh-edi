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
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.Exceptions;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly B2BContext _b2BContext;
        private readonly ProcessContext _processContext;
        private readonly ActorMessageQueueContext _actorMessageQueueContext;
        private IDbContextTransaction? _dbContextTransaction;

        public UnitOfWork(B2BContext b2BContext, ProcessContext processContext, ActorMessageQueueContext actorMessageQueueContext)
        {
            _b2BContext = b2BContext;
            _processContext = processContext;
            _actorMessageQueueContext = actorMessageQueueContext;
        }

        public async Task BeginTransactionAsync()
        {
            _dbContextTransaction = await _processContext.Database.BeginTransactionAsync().ConfigureAwait(false);
            await _b2BContext.Database.UseTransactionAsync(_dbContextTransaction.GetDbTransaction()).ConfigureAwait(false);
            await _actorMessageQueueContext.Database.UseTransactionAsync(_dbContextTransaction.GetDbTransaction()).ConfigureAwait(false);
        }

        public async Task CommitTransactionAsync()
        {
            if (_dbContextTransaction == null) throw new DBTransactionNotInitializedException();

            await _b2BContext.SaveChangesAsync().ConfigureAwait(false);
            await _processContext.SaveChangesAsync().ConfigureAwait(false);
            await _actorMessageQueueContext.SaveChangesAsync().ConfigureAwait(false);
            await _dbContextTransaction.CommitAsync().ConfigureAwait(false);
        }

        public async Task RollbackAsync()
        {
            if (_dbContextTransaction == null) throw new DBTransactionNotInitializedException();

            await _dbContextTransaction.RollbackAsync().ConfigureAwait(false);
        }
    }
}
