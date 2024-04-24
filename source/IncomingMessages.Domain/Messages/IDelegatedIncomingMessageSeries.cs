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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

/// <summary>
/// Represents a delegated incoming message series, with the data needed for a delegation
/// </summary>
public interface IDelegatedIncomingMessageSeries
{
    /// <summary>
    /// Who the incoming message series is delegated by (who originally was supposed to send the message)
    /// </summary>
    public ActorNumber? OriginalActorNumber { get; }

    /// <summary>
    /// The role of the actor which the series is delegated to
    ///     (the one who sends the message, and the queue that should receive it)
    /// </summary>
    public ActorRole? RequestedByActorRole { get; }

    /// <summary>
    /// What grid areas the incoming message is delegated in (if any).
    ///     This is used if the GridArea is null and the message is delegated, then we need to know which grid areas
    ///     the message is delegated in, to make sure Wholesale only retrieves data in the delegated grid areas
    /// </summary>
    public IReadOnlyCollection<string> DelegatedGridAreas { get; }

    /// <summary>
    /// Sets the incoming message series as delegated
    /// </summary>
    public void DelegateSeries(ActorNumber originalActorNumber, ActorRole requestedByActorRole, IReadOnlyCollection<string> delegatedGridAreas);
}
