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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

/// <summary>
/// Represents an incoming message
/// </summary>
public interface IIncomingMessage
{
    /// <summary>
    /// Id of the incoming message
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// Receiver number of the incoming message
    /// </summary>
    string ReceiverNumber { get; }

    /// <summary>
    /// Receiver Role of the incoming message
    /// </summary>
    string ReceiverRoleCode { get; }

    /// <summary>
    /// Sender Number of the incoming message
    /// </summary>
    string SenderNumber { get; }

    /// <summary>
    /// Sender Role of the incoming message
    /// </summary>
    string SenderRoleCode { get; }

    /// <summary>
    /// Business Reason of the incoming message
    /// </summary>
    string BusinessReason { get; }

    /// <summary>
    /// Message Type of the incoming message
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// When the message was created
    /// </summary>
    string CreatedAt { get; }

    /// <summary>
    /// Business Type of the incoming message
    /// </summary>
    string? BusinessType { get; }

    /// <summary>
    /// Series of the incoming message
    /// </summary>
    IReadOnlyCollection<IIncomingMessageSeries> Series { get; }

    IReadOnlyCollection<MessageType> AllowedMessageTypes { get; }

    IReadOnlyCollection<BusinessReason> AllowedBusinessReasons { get; }

    IReadOnlyCollection<ActorRole> AllowedSenderRoles { get; }

    IReadOnlyList<MeteringPointId> MeteringPointIds { get; }
}
