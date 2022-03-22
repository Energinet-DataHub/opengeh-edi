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

using System.Collections.Generic;
using System.Threading.Tasks;
using B2B.CimMessageAdapter;

namespace MarketRoles.B2B.CimMessageAdapter.IntegrationTests.Stubs
{
    public class MarketActivityRecordForwarderStub : IMarketActivityRecordForwarder
    {
        private readonly List<MarketActivityRecord> _uncommittedItems = new();
        private readonly List<MarketActivityRecord> _committedItems = new();

        public IReadOnlyCollection<MarketActivityRecord> CommittedItems => _committedItems.AsReadOnly();

        public Task AddAsync(MarketActivityRecord marketActivityRecord)
        {
            _committedItems.Clear();
            _uncommittedItems.Add(marketActivityRecord);
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
