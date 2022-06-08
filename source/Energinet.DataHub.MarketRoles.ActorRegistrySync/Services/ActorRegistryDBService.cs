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
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Services;

public class ActorRegistryDbService : IDisposable
{
    private readonly SqlConnection _sqlConnection;
    private bool _disposed;

    public ActorRegistryDbService(string connectionString)
    {
        _sqlConnection = new SqlConnection(connectionString);
    }

    public SqlConnection SqlConnection => _sqlConnection;

    public async Task<IEnumerable<GridAreaLink>> GetGriAreaLinkAsync()
    {
        return await _sqlConnection.QueryAsync<GridAreaLink>(
            @"SELECT [GridLinkId]
                       ,[GridAreaId]
                   FROM [dbo].[GridAreaLinkInfo]").ConfigureAwait(false) ?? (IEnumerable<GridAreaLink>)Array.Empty<object>();
    }

    public async Task<IEnumerable<GridArea>> GetGridAreasAsync()
    {
        return await _sqlConnection.QueryAsync<GridArea>(
            @"SELECT [Code]
                       ,[Name]
                       ,[Active]
                       ,[ActorId]
                       ,[PriceAreaCode]
                       ,[Id]
                  FROM [dbo].[GridArea]").ConfigureAwait(false) ?? (IEnumerable<GridArea>)Array.Empty<object>();
    }

    public async Task<IEnumerable<Actor>> GetActorsAsync()
    {
        return await _sqlConnection.QueryAsync<Actor>(
            @"SELECT [IdentificationNumber]
                       ,[IdentificationType]
                       ,[Roles]
                       ,[Active]
                       ,[Id]
        FROM [dbo].[Actor]").ConfigureAwait(false) ?? (IEnumerable<Actor>)Array.Empty<object>();
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
}
