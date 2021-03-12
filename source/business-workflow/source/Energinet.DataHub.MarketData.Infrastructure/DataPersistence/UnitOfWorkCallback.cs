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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Energinet.DataHub.MarketData.Application.Common;

namespace Energinet.DataHub.MarketData.Infrastructure.DataPersistence
{
    public class UnitOfWorkCallback : IUnitOfWorkCallback
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly Dictionary<IDataModel, ICanInsertDataModel> _addedEntities = new Dictionary<IDataModel, ICanInsertDataModel>();
        private readonly Dictionary<IDataModel, ICanUpdateDataModel> _changedEntities = new Dictionary<IDataModel, ICanUpdateDataModel>();

        public UnitOfWorkCallback(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void RegisterAmended(IDataModel entity, ICanUpdateDataModel repository)
        {
            if (!_changedEntities.ContainsKey(entity))
            {
                _changedEntities.Add(entity, repository);
            }
        }

        public void RegisterNew(IDataModel entity, ICanInsertDataModel repository)
        {
            if (!_addedEntities.ContainsKey(entity))
            {
                _addedEntities.Add(entity, repository);
            }
        }

        public async Task CommitAsync()
        {
            _connectionFactory.ResetConnection();
            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (IDataModel entity in _addedEntities.Keys)
                {
                    await _addedEntities[entity].PersistCreationOfAsync(entity).ConfigureAwait(false);
                }

                foreach (IDataModel entity in _changedEntities.Keys)
                {
                    await _changedEntities[entity].PersistUpdateOfAsync(entity).ConfigureAwait(false);
                }

                scope.Complete();
                Clear();
            }
        }

        private void Clear()
        {
            _addedEntities.Clear();
            _changedEntities.Clear();
        }
    }
}
