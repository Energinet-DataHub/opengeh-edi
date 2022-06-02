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
using System.Linq;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync;

public static class SyncActors
{
    //TODO: Change to timer trigger
    [FunctionName("SyncActors")]
    public static void Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        SyncActorsFromExternalSourceToDb();
    }

    //TODO: Refactor to match reqs in MR
    private static void SyncActorsFromExternalSourceToDb()
    {
        var now = DateTime.Now;
        var connectionString =
            "INSERT CS FROM LOCALSETTINGS - THIS IS THE CS FROM SHARED REGISTRY";
        using var sqlConnection = new SqlConnection(connectionString);
        var meteringPointConnectionString =
            "INSERT CS FROM LOCALSETTINGS - THIS IS THE TARGET CS IN MR";
        using var meteringPointSqlConnection = new SqlConnection(meteringPointConnectionString);
        meteringPointSqlConnection.Open();
        var userActors = GetUserActors(meteringPointSqlConnection);
        using var transaction = meteringPointSqlConnection.BeginTransaction();
        meteringPointSqlConnection.Execute("DELETE FROM [dbo].[GridAreaLinks]", transaction: transaction);
        meteringPointSqlConnection.Execute("DELETE FROM [dbo].[GridAreas]", transaction: transaction);
        meteringPointSqlConnection.Execute("DELETE FROM [dbo].[UserActor]", transaction: transaction);
        meteringPointSqlConnection.Execute("DELETE FROM [dbo].[Actor]", transaction: transaction);

        var actors = GetActors(sqlConnection);
        foreach (var actor in actors)
        {
            meteringPointSqlConnection.Execute(
                "INSERT INTO [dbo].[Actor] ([Id],[IdentificationNumber],[IdentificationType],[Roles]) VALUES (@Id,@IdentificationNumber,@IdentificationType, @Roles)",
                new
                {
                    actor.Id,
                    actor.IdentificationNumber,
                    IdentificationType = GetType(actor.IdentificationType),
                    Roles = GetRoles(actor.Roles),
                },
                transaction);
        }

        var gridAreas = GetGridAreas(sqlConnection);
        foreach (var gridArea in gridAreas)
        {
            meteringPointSqlConnection.Execute(
                    "INSERT INTO [dbo].[GridAreas]([Id],[Code],[Name],[PriceAreaCode],[FullFlexFromDate],[ActorId]) VALUES (@Id, @Code, @Name, @PriceAreaCode, null, @ActorId)",
                    new { gridArea.Id, gridArea.Code, gridArea.Name, gridArea.PriceAreaCode, gridArea.ActorId },
                    transaction);
        }

        var gridAreaLinks = GetGridAreaLinks(sqlConnection);
        foreach (var gridAreaLink in gridAreaLinks)
        {
            meteringPointSqlConnection.Execute(
                    "INSERT INTO [dbo].[GridAreaLinks] ([Id],[GridAreaId]) VALUES (@GridLinkId ,@GridAreaId)",
                    new { gridAreaLink.GridLinkId, gridAreaLink.GridAreaId },
                    transaction);
        }

        foreach (var userActor in userActors)
        {
            meteringPointSqlConnection.Execute(
                "INSERT INTO [dbo].[UserActor] (UserId, ActorId) VALUES (@UserId, @ActorId)",
                new { userActor.UserId, userActor.ActorId },
                transaction);
        }

        transaction.Commit();
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

    private static IEnumerable<UserActor> GetUserActors(SqlConnection sqlConnection)
    {
        return sqlConnection.Query<UserActor>(
            @"SELECT UserId, ActorId
                   FROM [dbo].[UserActor]") ?? (IEnumerable<UserActor>)Array.Empty<object>();
    }

    private static IEnumerable<GridAreaLink> GetGridAreaLinks(SqlConnection sqlConnection)
    {
        return sqlConnection.Query<GridAreaLink>(
            @"SELECT [GridLinkId]
                       ,[GridAreaId]
                   FROM [dbo].[GridAreaLinkInfo]") ?? (IEnumerable<GridAreaLink>)Array.Empty<object>();
    }

    private static IEnumerable<GridArea> GetGridAreas(SqlConnection sqlConnection)
    {
        return sqlConnection.Query<GridArea>(
            @"SELECT [Code]
                       ,[Name]
                       ,[Active]
                       ,[ActorId]
                       ,[PriceAreaCode]
                       ,[Id]
                  FROM [dbo].[GridArea]") ?? (IEnumerable<GridArea>)Array.Empty<object>();
    }

    private static IEnumerable<Actor> GetActors(SqlConnection sqlConnection)
    {
        return sqlConnection.Query<Actor>(
            @"SELECT [IdentificationNumber]
                       ,[IdentificationType]
                       ,[Roles]
                       ,[Active]
                       ,[Id]
        FROM [dbo].[Actor]") ?? (IEnumerable<Actor>)Array.Empty<object>();
    }

    private static string GetType(int identificationType)
    {
        return identificationType == 1 ? "GLN" : "EIC";
    }
}
