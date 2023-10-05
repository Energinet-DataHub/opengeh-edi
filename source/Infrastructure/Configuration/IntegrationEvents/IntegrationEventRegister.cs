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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.Data.SqlClient;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;

public class IntegrationEventRegister
{
    private const int UniqueConstraintSqlException = 2627;

    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ISystemDateTimeProvider _dateTimeProvider;

    public IntegrationEventRegister(IDatabaseConnectionFactory databaseConnectionFactory, ISystemDateTimeProvider dateTimeProvider)
    {
        _databaseConnectionFactory = databaseConnectionFactory;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RegisterIntegrationEventResult> RegisterAsync(string eventId, string eventType)
    {
        using var dbConnection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        if (await EventIsAlreadyRegisteredAsync(dbConnection, eventId).ConfigureAwait(false))
            return RegisterIntegrationEventResult.EventIsAlreadyRegistered;

        try
        {
            await RegisterEventAsync(dbConnection, eventId, eventType, _dateTimeProvider.Now()).ConfigureAwait(false);
        }
        catch (SqlException sqlException)
        {
            if (sqlException.Number == UniqueConstraintSqlException && sqlException.Message.Contains($"Violation of PRIMARY KEY constraint 'PK_Inbox'. Cannot insert duplicate key in object 'dbo.ReceivedIntegrationEvents'", StringComparison.OrdinalIgnoreCase))
                return RegisterIntegrationEventResult.EventIsAlreadyRegistered;

            throw;
        }

        return RegisterIntegrationEventResult.EventRegistered;
    }

    private static Task RegisterEventAsync(IDbConnection dbConnection, string eventId, string eventType, Instant now)
    {
        return dbConnection.ExecuteAsync(
            "INSERT INTO [dbo].[ReceivedIntegrationEvents] ([Id], [EventType], [OccurredOn]) VALUES (@id, @eventType, @occuredOn)",
            new { id = eventId, eventType = eventType, occuredOn = now });
    }

    private static Task<bool> EventIsAlreadyRegisteredAsync(IDbConnection dbConnection, string eventId)
    {
        return dbConnection.ExecuteScalarAsync<bool>(
            "SELECT COUNT(1) FROM [dbo].[ReceivedIntegrationEvents] WHERE Id=@eventId",
            new { eventId });
    }
}
