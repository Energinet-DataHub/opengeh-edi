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

using System.Data.SqlClient;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

public static class ActorFactory
{
    public static void InsertActor(string connectionString, string b2CId)
    {
        using var connection = new SqlConnection(connectionString);
        using var command = new SqlCommand();

        command.CommandText = @"BEGIN
                                   IF NOT EXISTS (SELECT * FROM [dbo].[Actor] WHERE ActorNumber = @ActorNumber)
                                   BEGIN
                                       INSERT INTO [dbo].[Actor] ([Id], [ActorNumber], [ExternalId])
                                       VALUES (@Id, @ActorNumber, @ExternalId)
                                   END
                                END";
        command.Parameters.AddWithValue("@Id", "756768A4-64B4-4B66-A5ED-21BA3D64A59D");
        command.Parameters.AddWithValue("@ActorNumber", "5790000610976");
        command.Parameters.AddWithValue("@ExternalId", b2CId);
        command.Connection = connection;

        command.Connection.Open();
        command.ExecuteNonQuery();
    }
}
