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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogger;

/// <summary>
/// The entity type which an audit log entry is related to, used to categorize the log entry.
/// </summary>
public record AuditLogEntityType(string Identifier)
{
    /// <summary>
    /// Used when searching for archived messages or getting a specific archived message
    /// </summary>
    public static readonly AuditLogEntityType ArchivedMessage = new("ArchivedMessage");

    /// <summary>
    /// Used when peeking and dequeueing a bundle from the actor message queue and cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType Bundle = new("Bundle");

    /// <summary>
    /// Used when creating a new energy result request process.
    /// </summary>
    public static readonly AuditLogEntityType RequestAggregatedMeasureData = new("RequestAggregatedMeasureData");

    /// <summary>
    /// Used when creating a new wholesale services request process.
    /// </summary>
    public static readonly AuditLogEntityType RequestWholesaleServices = new("RequestWholesaleServices");

    /// <summary>
    /// Used when searching for orchestrations or getting status of a specific orchestration.
    /// </summary>
    public static readonly AuditLogEntityType Orchestration = new("Orchestration");

    /// <summary>
    /// Used when grid area owner are cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType GridAreaOwner = new("GridAreaOwner");

    /// <summary>
    /// Used when internal command are cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType InternalCommand = new("InternalCommand");

    /// <summary>
    /// Used when outbox message are cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType OutboxMessage = new("OutboxMessage");

    /// <summary>
    /// Used when received inbox event are cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType ReceivedInboxEvent = new("ReceivedInboxEvent");

    /// <summary>
    /// Used when received integration event are cleaned up.
    /// </summary>
    public static readonly AuditLogEntityType ReceivedIntegrationEvent = new("ReceivedIntegrationEvent");
}
