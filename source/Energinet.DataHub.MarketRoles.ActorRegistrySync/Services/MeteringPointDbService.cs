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
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class MeteringPointDbService : IDisposable
{
    private readonly SqlConnection _sqlConnection;
    private DbTransaction? _transaction;
    private bool _disposed;

    public MeteringPointDbService(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public SqlConnection SqlConnection => _sqlConnection;

    public async Task CleanUpAsync()
    {
        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[GridAreaLinks]", transaction: _transaction)
            .ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[GridAreas]", transaction: _transaction)
            .ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[UserActor]", transaction: _transaction)
            .ConfigureAwait(false);
        await _sqlConnection.ExecuteAsync("DELETE FROM [dbo].[Actor]", transaction: _transaction)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<UserActor>> GetUserActorsAsync()
    {
        return await _sqlConnection.QueryAsync<UserActor>(
            @"SELECT UserId, ActorId
                   FROM [dbo].[UserActor]").ConfigureAwait(false) ?? (IEnumerable<UserActor>)Array.Empty<object>();
    }

    public async Task InsertGriAreaLinkAsync(IEnumerable<GridAreaLink> gridAreaLinks)
    {
        if (gridAreaLinks == null) throw new ArgumentNullException(nameof(gridAreaLinks));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        foreach (var gridAreaLink in gridAreaLinks)
        {
            await _sqlConnection.ExecuteAsync(
                "INSERT INTO [dbo].[GridAreaLinks] ([Id],[GridAreaId]) VALUES (@GridLinkId ,@GridAreaId)",
                new { gridAreaLink.GridLinkId, gridAreaLink.GridAreaId },
                _transaction).ConfigureAwait(false);
        }
    }

    public async Task InsertActorsAsync(IEnumerable<Actor> actors)
    {
        if (actors == null) throw new ArgumentNullException(nameof(actors));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        foreach (var actor in actors)
        {
            await _sqlConnection.ExecuteAsync(
                "INSERT INTO [dbo].[Actor] ([Id],[IdentificationNumber],[IdentificationType],[Roles]) VALUES (@Id,@IdentificationNumber,@IdentificationType, @Roles)",
                new
                {
                    actor.Id, actor.IdentificationNumber, IdentificationType = GetType(actor.IdentificationType), Roles = GetRoles(actor.Roles),
                },
                _transaction).ConfigureAwait(false);
        }
    }

    public async Task InsertGridAreasAsync(IEnumerable<GridArea> gridAreas)
    {
        if (gridAreas == null) throw new ArgumentNullException(nameof(gridAreas));

        if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);

        foreach (var gridArea in gridAreas)
        {
            await _sqlConnection.ExecuteAsync(
                "INSERT INTO [dbo].[GridAreas]([Id],[Code],[Name],[PriceAreaCode],[FullFlexFromDate],[ActorId]) VALUES (@Id, @Code, @Name, @PriceAreaCode, null, @ActorId)",
                new
                {
                    gridArea.Id,
                    gridArea.Code,
                    gridArea.Name,
                    gridArea.PriceAreaCode,
                    gridArea.ActorId,
                },
                _transaction).ConfigureAwait(false);
        }
    }

    public async Task InsertUserActorsAsync(IEnumerable<UserActor> userActors)
    {
        if (userActors == null) throw new ArgumentNullException(nameof(userActors));
        {
            if (_transaction == null) await BeginTransactionAsync().ConfigureAwait(false);
            foreach (var userActor in userActors)
            {
                await _sqlConnection.ExecuteAsync(
                    "INSERT INTO [dbo].[UserActor] (UserId, ActorId) VALUES (@UserId, @ActorId)",
                    new { userActor.UserId, userActor.ActorId },
                    _transaction).ConfigureAwait(false);
            }
        }
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
            case "DDZ": return "MeteringPointAdministrator";
            case "EZ": return "SystemOperator";
            case "MDR": return "MeteredDataResponsible";
            default: throw new InvalidOperationException("Role not known: " + ediRole);
        }
    }

    private async Task BeginTransactionAsync()
    {
        await _sqlConnection.OpenAsync().ConfigureAwait(false);
        _transaction = await _sqlConnection.BeginTransactionAsync().ConfigureAwait(false);
    }
}
