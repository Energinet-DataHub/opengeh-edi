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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

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

    public async Task SynchronizeAsync(IReadOnlyCollection<LegacyActor> actors)
    {
        await BeginTransactionAsync().ConfigureAwait(false);
        await InsertActorsAsync(actors).ConfigureAwait(false);
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

    private static IEnumerable<EnergySupplier> MapActorsToEnergySuppliers(IEnumerable<LegacyActor> actors)
    {
        return actors.Where(actor => actor.Roles.Contains("DDQ", StringComparison.InvariantCultureIgnoreCase)).Select(actor => new EnergySupplier(actor.Id, actor.IdentificationNumber));
    }

    private static string GetType(int identificationType)
    {
        return identificationType == 1 ? "GLN" : "EIC";
    }

    private static string GetRoles(string actorRoles)
    {
        return string.Join(
            ',',
            actorRoles.Split(
                    ',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(MapRole));
    }

    private static string MapRole(string ediRole)
    {
        switch (ediRole)
        {
            case "DDK": return "BalanceResponsibleParty";
            case "DDM": return "GridAccessProvider";
            case "DDQ": return "BalancePowerSupplier";
            case "DDX": return "ImBalanceSettlementResponsible";
            case "DDZ": return "MeteringPointAdministrator";
            case "DEA": return "MeteredDataAggregator";
            case "EZ": return "SystemOperator";
            case "MDR": return "MeteredDataResponsible";
            case "STS": return "DanishEnegeryAgency";
            default: throw new InvalidOperationException("Role not known: " + ediRole);
        }
    }

    private async Task InsertActorsAsync(IReadOnlyCollection<LegacyActor> actors)
    {
        if (actors == null) throw new ArgumentNullException(nameof(actors));

        var stringBuilder = new StringBuilder();
        foreach (var actor in actors)
        {
            string sql = $@"BEGIN
	                            IF NOT EXISTS (SELECT * FROM [dbo].[Actor]
					                            WHERE Id = CONVERT(uniqueidentifier, '{actor.Id}'))
                                BEGIN
                                    INSERT INTO [dbo].[Actor] ([Id],[IdentificationNumber],[IdentificationType],[Roles])
                                    VALUES ('{actor.Id}', '{actor.IdentificationNumber}', '{GetType(actor.IdentificationType)}', '{GetRoles(actor.Roles)}')
                                END
                            END";

            stringBuilder.Append(sql);
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
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
