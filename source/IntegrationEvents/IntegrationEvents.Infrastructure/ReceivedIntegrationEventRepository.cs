﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Threading.Tasks;
using BuildingBlocks.Application.DateTime;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure;

public class ReceivedIntegrationEventRepository : IReceivedIntegrationEventRepository
{
    // Error code can be found here: https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors-2000-to-2999?view=sql-server-ver16
    private const int UniqueConstraintSqlException = 2627;

    private readonly ISystemDateTimeProvider _dateTimeProvider;

    public ReceivedIntegrationEventRepository(ISystemDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AddReceivedIntegrationEventResult> AddIfNotExistsAsync(Guid eventId, string eventType, IDbConnection dbConnection, IDbTransaction dbTransaction)
    {
        try
        {
            await dbConnection.ExecuteAsync(
                    "INSERT INTO [dbo].[ReceivedIntegrationEvents] ([Id], [EventType], [OccurredOn]) VALUES (@id, @eventType, @occuredOn)",
                    new { id = eventId.ToString(), eventType = eventType, occuredOn = _dateTimeProvider.Now() },
                    dbTransaction)
                .ConfigureAwait(false);
        }
        catch (SqlException sqlException)
        {
            if (sqlException.Number == UniqueConstraintSqlException && sqlException.Message.Contains($"Violation of PRIMARY KEY constraint 'PK_Inbox'. Cannot insert duplicate key in object 'dbo.ReceivedIntegrationEvents'", StringComparison.OrdinalIgnoreCase))
                return AddReceivedIntegrationEventResult.EventIsAlreadyRegistered;

            throw;
        }

        return AddReceivedIntegrationEventResult.EventRegistered;
    }
}
