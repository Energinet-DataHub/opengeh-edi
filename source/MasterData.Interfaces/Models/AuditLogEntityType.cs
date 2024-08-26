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

namespace Energinet.DataHub.EDI.MasterData.Interfaces.Models;

/// <summary>
/// The activity which an audit log entry is related to, used to categorize the log entry.
/// </summary>
public enum AuditLogEntityType
{
    /// <summary>
    /// Used when searching for archived messages or getting a specific archived message
    /// </summary>
    ArchivedMessage,

    /// <summary>
    /// Used when peeking and dequeueing a bundle from the actor message queue.
    /// </summary>
    Bundle,

    /// <summary>
    /// Used when creating a new energy result request process.
    /// </summary>
    RequestAggregatedMeasureDataProcess,

    /// <summary>
    /// Used when creating a new wholesale services request process.
    /// </summary>
    RequestWholesaleServicesProcess,

    /// <summary>
    /// Used when searching for orchestrations or getting status of a specific orchestration.
    /// </summary>
    Orchestration,
}
