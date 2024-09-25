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

using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

public class EdiActorDriver
{
    private readonly string _connectionString;

    public EdiActorDriver(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> ActorExistsAsync(string actorNumber, string actorId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand();

        command.CommandText = "SELECT COUNT(*) FROM [Actor] WHERE ActorNumber = @ActorNumber AND ExternalId = @ExternalId";
        command.Parameters.AddWithValue("@ActorNumber", actorNumber);
        command.Parameters.AddWithValue("@ExternalId", actorId);
        command.Connection = connection;

        await command.Connection.OpenAsync().ConfigureAwait(false);
        return await command.ExecuteScalarAsync().ConfigureAwait(false) != null;
    }
}
