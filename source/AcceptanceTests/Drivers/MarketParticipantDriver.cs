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

using System.Data.SqlClient;
using Google.Protobuf;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class MarketParticipantDriver
{
    private readonly IntegrationEventPublisher _integrationEventPublisher;
    private readonly string _connectionString;

    public MarketParticipantDriver(IntegrationEventPublisher integrationEventPublisher, string connectionString)
    {
        _integrationEventPublisher = integrationEventPublisher;
        _connectionString = connectionString;
    }

    public async Task PublishActorActivatedAsync(string actorNumber, string b2CId)
    {
        await _integrationEventPublisher.PublishAsync(
            "ActorActivated",
            ActorFactory.CreateActorActivated(actorNumber, b2CId).ToByteArray()).ConfigureAwait(false);
    }

    public async Task ActorActivatedExistsAsync(string actorNumber, string azpToken)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();

        command.CommandText = "SELECT COUNT(*) FROM [Table] WHERE ActorNumber = @ActorNumber AND ExternalId = @ExternalId";
        command.Parameters.AddWithValue("@ActorNumber", actorNumber);
        command.Parameters.AddWithValue("@ExternalId", azpToken);
        command.Connection = connection;

        await command.Connection.OpenAsync().ConfigureAwait(false);
        var exist = await command.ExecuteScalarAsync().ConfigureAwait(false);
        Assert.NotNull(exist);
    }
}
