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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;

/// <summary>
/// This responsible managing resilient database transactions using Entity Framework Core.
/// It uses the execution strategy from a DbContext instance to handle retries for transient failures.
/// </summary>
public class ResilientTransaction
{
    private readonly DbContext _context;

    private ResilientTransaction(DbContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    public static ResilientTransaction New(DbContext context) => new(context);

    /// <summary>
    /// This method initiates a transaction across multiple DbContext instances, committing all changes atomically.
    /// If any operation fails, the transaction is rolled back and retried according to the execution strategy.
    /// </summary>
    /// <param name="contexts"></param>
    public async Task SaveChangesAsync(DbContext[] contexts)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            foreach (var dbContext in contexts)
            {
                await dbContext.Database.UseTransactionAsync(transaction.GetDbTransaction()).ConfigureAwait(false);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
