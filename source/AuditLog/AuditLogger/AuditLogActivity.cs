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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogger;

/// <summary>
/// An activity which an audit log entry is related to, describing the activity (an action, event etc.) that is being logged.
/// </summary>
public record AuditLogActivity(string Identifier, bool AuthenticatedUserRequired = true)
{
    public static readonly AuditLogActivity RequestEnergyResults = new("RequestEnergyResults");
    public static readonly AuditLogActivity RequestWholesaleResults = new("RequestWholesaleResults");
    public static readonly AuditLogActivity RequestCalculationResults = new("RequestCalculationResults");

    public static readonly AuditLogActivity ArchivedMessagesSearch = new("ArchivedMessagesSearch");
    public static readonly AuditLogActivity ArchivedMessagesGet = new("ArchivedMessagesGet");

    public static readonly AuditLogActivity MeteringPointArchivedMessageSearch = new("MeteringPointArchivedMessageSearch");
    public static readonly AuditLogActivity MeteringPointArchivedMessageGet = new("MeteringPointArchivedMessageGet");

    public static readonly AuditLogActivity OrchestrationsSearch = new("OrchestrationsSearch");
    public static readonly AuditLogActivity OrchestrationsGet = new("OrchestrationsGet");
    public static readonly AuditLogActivity OrchestrationsTerminate = new("OrchestrationsTerminate");

    public static readonly AuditLogActivity Dequeue = new("Dequeue");
    public static readonly AuditLogActivity Peek = new("Peek");

    public static readonly AuditLogActivity RetentionDeletion = new("RetentionDeletion", AuthenticatedUserRequired: false);
}
