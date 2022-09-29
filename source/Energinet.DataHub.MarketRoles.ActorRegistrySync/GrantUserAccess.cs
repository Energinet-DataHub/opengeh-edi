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
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.MarketRoles.ActorRegistrySync.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync;

public static class GrantUserAccess
{
    [FunctionName("GrantUserAccess")]
    public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        if (req == null) throw new ArgumentNullException(nameof(req));
        if (log == null) throw new ArgumentNullException(nameof(log));

        log.LogInformation("C# HTTP trigger function processed a request.");

        using var streamReader = new StreamReader(req.Body);
        var requestBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);
        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, };
        var data = JsonSerializer.Deserialize<UserActorDto>(requestBody, serializerOptions);

        if (data == null || data.UserObjectId == null || data.GlnNumbers == null)
        {
            return new BadRequestObjectResult("Data invalid or properties missing.");
        }

        var userObjectId = new Guid(data.UserObjectId);

        var meteringPointConnectionString = Environment.GetEnvironmentVariable("METERINGPOINT_DB_CONNECTION_STRING");
        using var meteringPointSqlConnection = new SqlConnection(meteringPointConnectionString);

        var actorIds = (await GetActorIdsByGlnNumbersAsync(meteringPointSqlConnection, data.GlnNumbers).ConfigureAwait(false)).ToList();
        var existingActorIdsForUser = await GetExistingActorIdsAsync(meteringPointSqlConnection, userObjectId, actorIds).ConfigureAwait(false);
        var remainingActorIds = actorIds.Where(id => !existingActorIdsForUser.Contains(id)).ToList();

        var userCreatedCount = await CreateUserAsync(meteringPointSqlConnection, userObjectId).ConfigureAwait(false);
        var permissionsCreatedCount = await CreateUserActorPermissionsAsync(meteringPointSqlConnection, userObjectId, remainingActorIds).ConfigureAwait(false);

        return new OkObjectResult($"User permissions updated. \n" +
                                  $"Created {userCreatedCount} new user(s).\n" +
                                  $"Created {permissionsCreatedCount} new permission(s)");
    }

    private static async Task<int> CreateUserAsync(IDbConnection sqlConnection, Guid userId)
    {
        var rowsAffected = await sqlConnection.ExecuteAsync(
            @"
            BEGIN
                IF NOT EXISTS (
                    SELECT * FROM [dbo].[User]
                    WHERE Id = @userId
                )
                BEGIN
                    INSERT INTO [dbo].[User] (Id)
                    VALUES (@userId)
                END
            END",
            new { userId }).ConfigureAwait(false);

        // Return 0 instead of -1 when no rows have been affected
        return Math.Max(rowsAffected, 0);
    }

    private static async Task<IEnumerable<Guid>> GetExistingActorIdsAsync(SqlConnection sqlConnection, Guid userObjectId, IEnumerable<Guid> actorIds)
    {
        return await sqlConnection.QueryAsync<Guid>(
            @"SELECT ActorId FROM [dbo].[UserActor]
               WHERE UserId = @userObjectId
               AND ActorId IN @actorIds",
            new { userObjectId, actorIds }).ConfigureAwait(false);
    }

    private static async Task<int> CreateUserActorPermissionsAsync(IDbConnection sqlConnection, Guid userId, IEnumerable<Guid> actorIds)
    {
        var userActorParams = actorIds.Select(actorId => new UserActorParam(userId, actorId));

        var rowsAffected = await sqlConnection.ExecuteAsync("INSERT INTO [dbo].[UserActor] (UserId, ActorId) VALUES (@UserId, @ActorId)", userActorParams).ConfigureAwait(false);

        // Return 0 instead of -1 when no rows have been affected
        return Math.Max(rowsAffected, 0);
    }

    private static async Task<IEnumerable<Guid>> GetActorIdsByGlnNumbersAsync(IDbConnection sqlConnection, IReadOnlyCollection<string> glnNumbers)
    {
        return await sqlConnection.QueryAsync<Guid>(
            @"SELECT Id
                   FROM [dbo].[Actor] WHERE Actor.IdentificationNumber IN @glnNumbers",
            new { glnNumbers }).ConfigureAwait(false);
    }
}

internal record UserActorParam(Guid UserId, Guid ActorId);
