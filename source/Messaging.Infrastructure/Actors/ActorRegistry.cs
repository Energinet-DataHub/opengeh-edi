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
using Dapper;
using Messaging.Application.Actors;
using Messaging.Application.Configuration.DataAccess;
using Microsoft.Data.SqlClient;

namespace Messaging.Infrastructure.Actors;

public class ActorRegistry : IActorRegistry
{
    private readonly IEdiDatabaseConnection _ediDatabaseConnection;

    public ActorRegistry(IEdiDatabaseConnection ediDatabaseConnection)
    {
        _ediDatabaseConnection = ediDatabaseConnection;
    }

    public async Task<Guid?> IfActorExistsGetB2CIdAsync(string identificationNumber)
    {
        using var connection = await _ediDatabaseConnection.GetConnectionAndOpenAsync().ConfigureAwait(false);
        if (identificationNumber == null) throw new ArgumentNullException(nameof(identificationNumber));
        var sqlStatement = @$"SELECT B2CId FROM [b2b].[Actor]  WHERE IdentificationNumber = @IdentificationNumber";
        var b2CId = await connection.QueryFirstOrDefaultAsync<Guid?>(sqlStatement, new { IdentificationNumber = identificationNumber }).ConfigureAwait(false);
        return b2CId;
    }

    public async Task<bool> TryStoreAsync(CreateActor createActor)
    {
        if (createActor == null) throw new ArgumentNullException(nameof(createActor));
        using var connection = await _ediDatabaseConnection.GetConnectionAndOpenAsync().ConfigureAwait(false);
        var sqlStatement = @$"INSERT INTO [b2b].[Actor] ([Id], [B2CId], [IdentificationNumber]) VALUES ('{createActor.ActorId}', '{createActor.B2CId}', '{createActor.IdentificationNumber}')";
        try
        {
            await connection.ExecuteAsync(sqlStatement)
                .ConfigureAwait(false);
        }
        catch (SqlException)
        {
            return false;
        }

        return true;
    }
}
