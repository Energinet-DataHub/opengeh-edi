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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventProcessors;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;

public class IntegrationEventRegister
{
    private readonly B2BContext _context;
    private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly IReadOnlyCollection<IIntegrationEventProcessor> _processors;

    public IntegrationEventRegister(B2BContext context, IDatabaseConnectionFactory databaseConnectionFactory, ISystemDateTimeProvider dateTimeProvider, IEnumerable<IIntegrationEventProcessor> processors)
    {
        _context = context;
        _databaseConnectionFactory = databaseConnectionFactory;
        _dateTimeProvider = dateTimeProvider;
        _processors = new ReadOnlyCollection<IIntegrationEventProcessor>(processors.ToList());
    }

    public async Task<RegisterIntegrationEventResult> RegisterAsync(string eventId, string eventType)
    {
        using var dbConnection = await _databaseConnectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        if (await EventIsAlreadyRegisteredAsync(dbConnection, eventId).ConfigureAwait(false))
            return RegisterIntegrationEventResult.EventIsAlreadyRegistered;

        await RegisterEventAsync(dbConnection, eventId, eventType, _dateTimeProvider.Now()).ConfigureAwait(false);

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
