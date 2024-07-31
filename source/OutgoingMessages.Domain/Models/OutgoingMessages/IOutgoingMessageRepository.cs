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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

/// <summary>
/// Store for outgoing actor messages
/// </summary>
public interface IOutgoingMessageRepository
{
    /// <summary>
    /// Add outgoing message to database and file storage
    /// </summary>
    Task AddAsync(OutgoingMessage message);

    /// <summary>
    /// Get all messages assigned to a bundle by id
    /// </summary>
    Task<OutgoingMessageBundle> GetAsync(PeekResult peekResult);

    /// <summary>
    /// Checks if a message exists in the database for the designated receiver and the external id.
    /// </summary>
    Task<OutgoingMessage?> GetAsync(Receiver receiver, ExternalId externalId);

    /// <summary>
    /// Delete outgoing messages if they exists
    /// </summary>
    Task DeleteOutgoingMessagesIfExistsAsync(IReadOnlyCollection<BundleId> bundleMessageIds);
}
