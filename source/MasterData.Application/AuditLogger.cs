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

using System.Buffers.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;

namespace Energinet.DataHub.EDI.MasterData.Application;

public class AuditLogger(IAuditUserContext auditUserContext) : IAuditLogger
{
    private readonly IAuditUserContext _auditUserContext = auditUserContext;

    public Task LogAsync(
        AuditLogId id,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType affectedEntityType,
        string affectedEntityKey)
    {
        var currentUser = _auditUserContext.CurrentUser;

        var userId = currentUser?.UserId ?? Guid.Empty;
        var actorId = currentUser?.ActorId ?? Guid.Empty;
        var permissions = currentUser?.Permissions;

        var payloadAsString = activityPayload switch
        {
            null => string.Empty,
            string payload => payload,
            _ => EncodeObjectAsBase64String(activityPayload),
        };

        Console.WriteLine(
            $"Audit log entry:\n"
            + $"\t{id.Id}, "
            + $"\t{userId}, "
            + $"\t{actorId}, "
            + $"\t{permissions}, "
            + $"\t{activity}, "
            + $"\t{activityOrigin}, "
            + $"\t{payloadAsString}, "
            + $"\t{affectedEntityType}, "
            + $"\t{affectedEntityKey}");

        return Task.CompletedTask;
    }

    private string EncodeObjectAsBase64String(object objectToEncode)
    {
        var jsonString = JsonSerializer.Serialize(objectToEncode);

        var byteArray = Encoding.UTF8.GetBytes(jsonString);

        return Convert.ToBase64String(byteArray);
    }
}
