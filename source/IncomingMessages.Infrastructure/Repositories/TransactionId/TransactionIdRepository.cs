﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.IncomingMessages.Domain.TransactionId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.TransactionId;

public class TransactionIdRepository : ITransactionIdRepository
{
    private readonly IncomingMessagesContext _incomingMessagesContext;

    public TransactionIdRepository(IncomingMessagesContext incomingMessagesContext)
    {
        _incomingMessagesContext = incomingMessagesContext;
    }

    public async Task<IReadOnlyList<string>> GetDuplicatedTransactionIdsAsync(
        string senderId,
        IReadOnlyCollection<string> transactionIds,
        CancellationToken cancellationToken)
    {
        var duplicatedTransactionIdsForSender = await GetDuplicatedTransactionsFromDbAsync(senderId, transactionIds, cancellationToken)
            .ConfigureAwait(false);
        if (!transactionIds.Any())
            duplicatedTransactionIdsForSender = GetDuplicatedTransactionsFromInMemoryCollection(senderId, transactionIds);

        return duplicatedTransactionIdsForSender.Select(x => x.TransactionId).ToList();
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

    private IReadOnlyList<TransactionIdForSender> GetDuplicatedTransactionsFromInMemoryCollection(
        string senderId,
        IReadOnlyCollection<string> transactionIds)
    {
        return _incomingMessagesContext.TransactionIdForSenders.Local
            .Where(
                transactionIdForSender => transactionIds.Contains(transactionIdForSender.TransactionId)
                                          && transactionIdForSender.SenderId == senderId)
            .ToList();
    }

    private async Task<IReadOnlyList<TransactionIdForSender>> GetDuplicatedTransactionsFromDbAsync(
        string senderId,
        IReadOnlyCollection<string> transactionIds,
        CancellationToken cancellationToken)
    {
        return await _incomingMessagesContext.TransactionIdForSenders
            .AsNoTracking()
            .Where(transactionIdForSender => transactionIds.Contains(transactionIdForSender.TransactionId)
                                             && transactionIdForSender.SenderId == senderId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
