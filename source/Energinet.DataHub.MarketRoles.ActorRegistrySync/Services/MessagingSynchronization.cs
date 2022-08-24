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
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class MessagingSynchronization : IDisposable
{
    private readonly SqlConnection _sqlConnection;
    private bool _disposed;
    private DbTransaction? _transaction;

    public MessagingSynchronization(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task SynchronizeAsync(IReadOnlyCollection<LegacyActor> actors)
    {
        await BeginTransactionAsync().ConfigureAwait(false);
        await InsertActorsAsync(actors).ConfigureAwait(false);
        await CommitAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _sqlConnection.Dispose();
        }

        _disposed = true;
    }

    private static string GetType(int identificationType)
    {
        return identificationType == 1 ? "GLN" : "EIC";
    }

    private async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync().ConfigureAwait(false);
        }
    }

    private async Task BeginTransactionAsync()
    {
        await _sqlConnection.OpenAsync().ConfigureAwait(false);
        _transaction = await _sqlConnection.BeginTransactionAsync().ConfigureAwait(false);
    }

    private async Task InsertActorsAsync(IEnumerable<LegacyActor> actors)
    {
        if (actors == null) throw new ArgumentNullException(nameof(actors));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        var stringBuilder = new StringBuilder();
        foreach (var actor in actors)
        {
            string sql = $@"  BEGIN
	                            IF NOT EXISTS (SELECT * FROM [b2b].[Actor]
					                            WHERE Id = '{actor.Id}')
	                            BEGIN
		                            INSERT INTO [b2b].[Actor] ([Id],[IdentificationNumber],[IdentificationType])
		                            VALUES ('{actor.Id}', '{actor.IdentificationNumber}', '{GetType(actor.IdentificationType)}')
	                            END
                               END";

            stringBuilder.Append(sql);
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
    }
}
