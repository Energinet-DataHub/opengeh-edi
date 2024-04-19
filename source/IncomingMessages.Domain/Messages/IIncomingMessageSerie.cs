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

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

/// <summary>
/// Represents a series of an incoming message
/// </summary>
public interface IIncomingMessageSerie
{
    /// <summary>
    /// Id of the incoming message series
    /// </summary>
    public string TransactionId { get; }

    /// <summary>
    /// Start Date and Time of the incoming message series
    /// </summary>
    public string StartDateTime { get; }

    /// <summary>
    /// End Date and Time of the incoming message series
    /// </summary>
    public string? EndDateTime { get; }

    /// <summary>
    /// Grid Area of the incoming message series
    /// </summary>
    public string? GridArea { get; }

    /// <summary>
    /// Who the incoming message series is delegated by (who originally was supposed to send the message)
    /// </summary>
    public ActorNumber? DelegatedByActorNumber { get; }

    /// <summary>
    /// The role of the actor which the series is delegated to
    ///     (the one who sends the message, and the queue that should receive it)
    /// </summary>
    public ActorRole? DelegatedToActorRole { get; }

    /// <summary>
    /// What grid areas the incoming message is delegated in (if any).
    ///     This is used if the GridArea is null and the message is delegated, then we need to know which grid areas
    ///     the message is delegated in, to make sure Wholesale only retrieves data in the delegated grid areas
    /// </summary>
    public IReadOnlyCollection<string> DelegatedGridAreas { get; }

    /// <summary>
    /// Sets the incoming message series as delegated
    /// </summary>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The domain term is delegate, so we should use that")]
    public void Delegate(ActorNumber delegatedByActorNumber, ActorRole delegatedToActorRole, IReadOnlyCollection<string> delegatedGridAreas);
}
