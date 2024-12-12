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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.TransactionId;

/// <summary>
/// Store containing transaction id for all received market activity records
/// </summary>
public interface ITransactionIdRepository
{
    /// <summary>
    /// Returns a list of existing <paramref name="transactionIds"/> if they already is registered by the sender <paramref name="senderId"/>
    /// </summary>
    /// <param name="senderId"></param>
    /// <param name="transactionIds"></param>
    /// <param name="cancellationToken"></param>
    Task<IReadOnlyList<string>> TransactionIdExistsAsync(string senderId, IReadOnlyCollection<string> transactionIds, CancellationToken cancellationToken);

    /// <summary>
    /// Store transaction ids for the specified sender
    /// </summary>
    Task AddAsync(
        string senderId,
        IReadOnlyCollection<string> transactionIds,
        CancellationToken cancellationToken);
}
