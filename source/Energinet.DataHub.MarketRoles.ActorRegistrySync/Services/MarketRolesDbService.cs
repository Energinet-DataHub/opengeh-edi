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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class MarketRolesDbService : IDisposable
{
    private readonly SqlConnection _sqlConnection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public MarketRolesDbService(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public async Task CleanUpAsync()
    {
        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[Actor]", transaction: _transaction)
            .ConfigureAwait(false);

        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[SupplierRegistrations]", transaction: _transaction)
            .ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[EnergySuppliers]", transaction: _transaction)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<SupplierRegistration>> GetSupplierRegistrationsAsync()
    {
        return await _sqlConnection.QueryAsync<SupplierRegistration>(
            @"SELECT Id,
                        EnergySupplierId,
                        BusinessProcessId,
                        StartOfSupplyDate,
                        EndOfSupplyDate,
                        AccountingPointId
        FROM [dbo].[SupplierRegistrations]").ConfigureAwait(false) ?? (IEnumerable<SupplierRegistration>)Array.Empty<object>();
    }

    public async Task InsertActorsAsync(IEnumerable<Actor> actors)
    {
        if (actors == null) throw new ArgumentNullException(nameof(actors));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        var stringBuilder = new StringBuilder();
        foreach (var actor in actors)
        {
            stringBuilder.Append(@"INSERT INTO [dbo].[Actor] ([Id],[IdentificationNumber],[IdentificationType],[Roles])
             VALUES ('" + actor.Id + "', '" + actor.IdentificationNumber + "', '" + GetType(actor.IdentificationType) + "', '" + GetRoles(actor.Roles) + "')");
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
    }

    public async Task InsertEnergySuppliersAsync(IEnumerable<EnergySupplier> energySuppliers)
    {
        if (energySuppliers == null) throw new ArgumentNullException(nameof(energySuppliers));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        var stringBuilder = new StringBuilder();
        foreach (var energySupplier in energySuppliers)
        {
            stringBuilder.Append(@"INSERT INTO [dbo].[EnergySuppliers] ([Id],[GlnNumber])
             VALUES ('" + energySupplier.Id + "', '" + energySupplier.IdentificationNumber + "')");
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
    }

    public async Task InsertSupplierRegistrationsAsync(IEnumerable<SupplierRegistration> supplierRegistrations)
    {
        if (supplierRegistrations == null) throw new ArgumentNullException(nameof(supplierRegistrations));

        var registrations = supplierRegistrations.ToList();

        if (!registrations.Any()) return;

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        var stringBuilder = new StringBuilder();
        var culture = new CultureInfo("da-DK");
        foreach (var supplierRegistration in registrations)
        {
            var startDate = supplierRegistration.StartOfSupplyDate != null ? $"'{supplierRegistration.StartOfSupplyDate.Value.ToString("o", culture)}'" : "null";
            var endDate = supplierRegistration.EndOfSupplyDate != null ? $"'{supplierRegistration.EndOfSupplyDate.Value.ToString("o", culture)}'" : "null";

            var insertSql = @$"INSERT INTO [dbo].[SupplierRegistrations] (Id, EnergySupplierId, BusinessProcessId, StartOfSupplyDate, EndOfSupplyDate, AccountingPointId)
             VALUES (
                 '{supplierRegistration.Id}',
                 '{supplierRegistration.EnergySupplierId}',
                 '{supplierRegistration.BusinessProcessId}',
                 {startDate},
                 {endDate},
                 '{supplierRegistration.AccountingPointId}')";

            stringBuilder.Append(insertSql);
            stringBuilder.AppendLine();
        }

        await _sqlConnection.ExecuteAsync(
            stringBuilder.ToString(),
            transaction: _transaction).ConfigureAwait(false);
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync().ConfigureAwait(false);
        }
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

    private async Task BeginTransactionAsync()
    {
        await _sqlConnection.OpenAsync().ConfigureAwait(false);
        _transaction = await _sqlConnection.BeginTransactionAsync().ConfigureAwait(false);
    }
}
