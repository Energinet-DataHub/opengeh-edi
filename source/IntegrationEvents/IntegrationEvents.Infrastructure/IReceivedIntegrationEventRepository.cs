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
using System.Data;
using System.Threading.Tasks;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure;

/// <summary>
/// Persists received integration event metadata
/// </summary>
public interface IReceivedIntegrationEventRepository
{
    /// <summary>
    /// Add a Received Integration Event to the database, if it doesn't already exists
    /// </summary>
    /// <returns>Returns true if the event was added to the database, and false if the event wasn't added because it already exists</returns>
    Task<AddReceivedIntegrationEventResult> AddIfNotExistsAsync(Guid eventId, string eventType, IDbConnection dbConnection, IDbTransaction dbTransaction);
}
