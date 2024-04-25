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
public interface IIncomingMessageSeries : IDelegatedIncomingMessageSeries
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
    /// Get the actor number from the series based on the role. Eg. if the role is EnergySupplier,
    /// the EnergySupplierId should be returned. Returns null if the data for the role is not present.
    /// </summary>
    public ActorNumber? GetActorNumberForRole(ActorRole actorRole);
}
