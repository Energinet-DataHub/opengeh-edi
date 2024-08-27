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

namespace Energinet.DataHub.EDI.AuditLog.AuditUser;

/// <summary>
/// Get the current user for audit logging
/// </summary>
public interface IAuditUserContext
{
    /// <summary>
    /// Get the current user for audit logging. This is typically the user that is currently logged in, and thus can
    /// be null if no user currently exists.
    /// </summary>
    AuditUser? CurrentUser { get; }
}
