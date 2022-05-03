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
using B2B.CimMessageAdapter.Transactions;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.Transactions;

namespace B2B.Transactions.IntegrationTests.CimMessageAdapter.Stubs
{
    public class TransactionQueueDispatcherStub : ITransactionQueueDispatcher
    {
        private readonly List<IncomingMessage> _uncommittedItems = new();
        private readonly List<IncomingMessage> _committedItems = new();

        public IReadOnlyCollection<IncomingMessage> CommittedItems => _committedItems.AsReadOnly();

        public Task AddAsync(IncomingMessage transaction)
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
