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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages
{
    public class TransactionIdRepository : ITransactionIdRepository
    {
        private readonly B2BContext _b2BContext;

        public TransactionIdRepository(B2BContext b2BContext)
        {
            _b2BContext = b2BContext;
        }

        public async Task<bool> TransactionIdExistsAsync(
            string senderId,
            string transactionId,
            CancellationToken cancellationToken)
        {
            var transaction = await GetTransactionFromDbAsync(senderId, transactionId, cancellationToken).ConfigureAwait(false)
                              ?? GetTransactionFromInMemoryCollection(senderId, transactionId);

            return transaction != null;
        }

        public async Task StoreAsync(
            string senderId,
            IReadOnlyList<string> transactionIds,
            CancellationToken cancellationToken)
        {
            if (transactionIds == null) throw new ArgumentNullException(nameof(transactionIds));

            foreach (var transactionId in transactionIds)
            {
               await _b2BContext.TransactionIds.AddAsync(new TransactionIdForSender(transactionId, senderId), cancellationToken).ConfigureAwait(false);
            }
        }

        private TransactionIdForSender? GetTransactionFromInMemoryCollection(string senderId, string transactionId)
        {
            return _b2BContext.TransactionIds.Local
                .FirstOrDefault(x => x.TransactionId == transactionId && x.SenderId == senderId);
        }

        private async Task<TransactionIdForSender?> GetTransactionFromDbAsync(string senderId, string transactionId, CancellationToken cancellationToken)
        {
            return await _b2BContext.TransactionIds
                .FirstOrDefaultAsync(
                    transactionIdForSender => transactionIdForSender.TransactionId == transactionId
                         && transactionIdForSender.SenderId == senderId,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
