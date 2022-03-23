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
using System.Threading.Tasks;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Transactions;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs
{
    public class TransactionQueueDispatcherStub : ITransactionQueueDispatcher
    {
        private readonly List<B2BTransaction> _uncommittedItems = new();
        private readonly List<B2BTransaction> _committedItems = new();

        public IReadOnlyCollection<B2BTransaction> CommittedItems => _committedItems.AsReadOnly();

        public Task AddAsync(B2BTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            _committedItems.Clear();
            _uncommittedItems.Add(transaction);
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            _committedItems.Clear();
            _committedItems.AddRange(_uncommittedItems);
            _uncommittedItems.Clear();
            return Task.CompletedTask;
        }
    }
}
