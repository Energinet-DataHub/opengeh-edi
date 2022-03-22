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
using System.Threading.Tasks;
using B2B.CimMessageAdapter.DataAccess;
using B2B.CimMessageAdapter.Message.MessageIds;

namespace B2B.CimMessageAdapter.Message.TransactionIds
{
    public class TransactionIdRegistry : ITransactionIds
    {
        private readonly MarketRolesContext _context;

        public TransactionIdRegistry(MarketRolesContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> TryStoreAsync(string transactionId)
        {
            var result = await _context.TransactionIds.FindAsync(transactionId).ConfigureAwait(false);

            if (result != null) return false;

            await _context.TransactionIds.AddAsync(new IncomingTransactionId(transactionId)).ConfigureAwait(false);
            return true;

            // await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
