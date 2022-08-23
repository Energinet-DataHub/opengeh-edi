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
using System.Text.Json.Serialization;
using Messaging.Application.Common.Commands;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting;

public class SendFailureNotification : InternalCommand
{
    [JsonConstructor]
    public SendFailureNotification(Guid id, Guid requestId, string idempotencyId, string failureDescription, string reason, string referenceId, string messageType, string requestedFormat)
        : base(id)
    {
        RequestId = requestId;
        IdempotencyId = idempotencyId;
        FailureDescription = failureDescription;
        Reason = reason;
        ReferenceId = referenceId;
        MessageType = messageType;
        RequestedFormat = requestedFormat;
    }

    public SendFailureNotification(Guid requestId, string idempotencyId, string failureDescription, string reason, string referenceId, string messageType, string requestedFormat)
    {
        RequestId = requestId;
        IdempotencyId = idempotencyId;
        FailureDescription = failureDescription;
        Reason = reason;
        ReferenceId = referenceId;
        MessageType = messageType;
        RequestedFormat = requestedFormat;
    }

    public Guid RequestId { get; }

    public string IdempotencyId { get; }

    public string FailureDescription { get; }

    public string Reason { get; }

    public string ReferenceId { get; }

    public string MessageType { get; }

    public string RequestedFormat { get; }
}
