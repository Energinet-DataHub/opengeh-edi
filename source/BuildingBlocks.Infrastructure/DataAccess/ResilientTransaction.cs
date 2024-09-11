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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;

/// <summary>
/// This is responsible for managing resilient database transactions using Entity Framework Core.
/// It uses the execution strategy from a DbContext instance to handle retries for transient failures.
/// If an action is specified, it will be executed before committing the transaction.
/// E.g. to ensure database transaction is only committed if sending a message to a message broker success.
/// </summary>
public class ResilientTransaction
{
    private readonly Func<Task>? _action;

    private ResilientTransaction(Func<Task>? action)
    {
        _action = action;
    }

    public static ResilientTransaction New(Func<Task>? action = null) => new(action);

    /// <summary>
    /// This method initiates a transaction across multiple DbContext instances,
    /// committing all changes atomically after action is successfully executed if specified.
    /// If any operation fails, the transaction is rolled back and retried according to the execution strategy.
    /// </summary>
    public async Task SaveChangesAsync(IReadOnlyCollection<DbContext> dbContexts)
    {
        if (dbContexts.Count == 0)
            throw new InvalidOperationException("Cannot save changes for empty DbContext collection");

        var firstDbContext = dbContexts.First();
        using (var transaction = await firstDbContext.Database.BeginTransactionAsync().ConfigureAwait(false))
        {
            using var dbTransaction = transaction.GetDbTransaction();

            if (_action != null) await _action().ConfigureAwait(false);

            foreach (var dbContext in dbContexts)
            {
                await dbContext.Database.UseTransactionAsync(dbTransaction).ConfigureAwait(false);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }
}
