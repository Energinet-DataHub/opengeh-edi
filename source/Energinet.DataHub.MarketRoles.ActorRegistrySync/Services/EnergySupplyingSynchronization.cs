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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class EnergySupplyingSynchronization : IDisposable
{
    private readonly SqlConnection _sqlConnection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public EnergySupplyingSynchronization(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task SynchronizeAsync(IReadOnlyCollection<Actor> actors)
    {
        await BeginTransactionAsync().ConfigureAwait(false);
        await DeleteEnergySupplierAndSupplierRegistrationsAsync().ConfigureAwait(false);
        await InsertEnergySuppliersAsync(MapActorsToEnergySuppliers(actors)).ConfigureAwait(false);
        await CommitTransactionAsync().ConfigureAwait(false);
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

    private static IEnumerable<EnergySupplier> MapActorsToEnergySuppliers(IEnumerable<Actor> actors)
    {
        return actors.Where(actor => actor.Roles != null && actor.Roles.Contains("12", StringComparison.InvariantCultureIgnoreCase)).Select(actor => new EnergySupplier(actor.Id, actor.IdentificationNumber));
    }

    private async Task DeleteEnergySupplierAndSupplierRegistrationsAsync()
    {
        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[SupplierRegistrations]", transaction: _transaction)
            .ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[EnergySuppliers]", transaction: _transaction)
            .ConfigureAwait(false);
    }

    private async Task InsertEnergySuppliersAsync(IEnumerable<EnergySupplier> energySuppliers)
    {
        if (energySuppliers == null) throw new ArgumentNullException(nameof(energySuppliers));

        var stringBuilder = new StringBuilder();
        foreach (var energySupplier in energySuppliers)
        {
            string sql = $@"  BEGIN
	                            IF NOT EXISTS (SELECT * FROM [dbo].[EnergySuppliers]
					                            WHERE Id = '{energySupplier.Id}')
	                            BEGIN
		                            INSERT INTO [dbo].[EnergySuppliers] ([Id],[GlnNumber])
		                            VALUES ('{energySupplier.Id}', '{energySupplier.IdentificationNumber}')
	                            END
                              END";

            stringBuilder.Append(sql);
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
    }

    private async Task BeginTransactionAsync()
    {
        await _sqlConnection.OpenAsync().ConfigureAwait(false);
        _transaction = await _sqlConnection.BeginTransactionAsync().ConfigureAwait(false);
    }

    private async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync().ConfigureAwait(false);
        }
    }
}
