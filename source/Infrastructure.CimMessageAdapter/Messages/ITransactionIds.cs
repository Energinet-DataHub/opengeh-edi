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
using System.Threading;
using System.Threading.Tasks;

namespace Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages
{
    /// <summary>
    /// Store containing transaction id for all received market activity records
    /// </summary>
    public interface ITransactionIds
    {
        /// <summary>
        /// Checks if <paramref name="transactionId"/> is already registered by the sender <paramref name="senderId"/>
        /// </summary>
        /// <param name="senderId"></param>
        /// <param name="transactionId"></param>
        /// <param name="cancellationToken"></param>
        Task<bool> TransactionIdExistsAsync(string senderId, string transactionId, CancellationToken cancellationToken);

        /// <summary>
        /// Store transaction ids for the specified sender
        /// </summary>
        Task StoreAsync(
            string senderId,
            IReadOnlyList<string> transactionIds,
            CancellationToken cancellationToken);
    }
}
