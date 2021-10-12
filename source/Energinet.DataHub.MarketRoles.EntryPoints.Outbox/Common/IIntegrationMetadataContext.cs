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
using NodaTime;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox.Common
{
    /// <summary>
    /// Context for the current fetched integration event scope
    /// </summary>
    public interface IIntegrationMetadataContext
    {
        /// <summary>
        /// Get the Timestamp.
        /// </summary>
        public Instant Timestamp { get; }

        /// <summary>
        /// Get the CorrelationId.
        /// </summary>
        public string? CorrelationId { get; }

        /// <summary>
        /// Get the EventId.
        /// </summary>
        public Guid EventId { get; }

        /// <summary>
        /// Set the initial meta data.
        /// </summary>
        public void SetMetadata(Instant timestamp, string correlationId, Guid eventId);
    }
}
