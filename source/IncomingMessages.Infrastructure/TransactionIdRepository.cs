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

using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

public class TransactionIdRepository : ITransactionIdRepository
{
    private readonly IncomingMessagesContext _incomingMessagesContext;

    public TransactionIdRepository(IncomingMessagesContext incomingMessagesContext)
    {
        _incomingMessagesContext = incomingMessagesContext;
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

    public async Task AddAsync(
        string senderId,
        IReadOnlyCollection<string> transactionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(transactionIds);

        foreach (var transactionId in transactionIds)
        {
           await _incomingMessagesContext.TransactionIdForSenders.AddAsync(new TransactionIdForSender(transactionId, senderId), cancellationToken).ConfigureAwait(false);
        }
    }

    private TransactionIdForSender? GetTransactionFromInMemoryCollection(string senderId, string transactionId)
    {
        return _incomingMessagesContext.TransactionIdForSenders.Local
            .FirstOrDefault(x => x.TransactionId == transactionId && x.SenderId == senderId);
    }

    private async Task<TransactionIdForSender?> GetTransactionFromDbAsync(string senderId, string transactionId, CancellationToken cancellationToken)
    {
        return await _incomingMessagesContext.TransactionIdForSenders
            .FirstOrDefaultAsync(
                transactionIdForSender => transactionIdForSender.TransactionId == transactionId
                     && transactionIdForSender.SenderId == senderId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
