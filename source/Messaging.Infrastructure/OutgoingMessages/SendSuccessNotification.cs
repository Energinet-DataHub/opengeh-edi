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
using System.Text.Json.Serialization;
using Messaging.Application.Common.Commands;

namespace Messaging.Infrastructure.OutgoingMessages;

public class SendSuccessNotification : InternalCommand
{
    [JsonConstructor]
    public SendSuccessNotification(Guid requestId, string idempotencyId, string referenceId, string messageType, Uri messageStorageLocation)
    {
        RequestId = requestId;
        IdempotencyId = idempotencyId;
        ReferenceId = referenceId;
        MessageType = messageType;
        MessageStorageLocation = messageStorageLocation;
    }

    public Guid RequestId { get; }

    public string IdempotencyId { get; }

    public string ReferenceId { get; }

    public string MessageType { get; }

    public Uri MessageStorageLocation { get; }
}
