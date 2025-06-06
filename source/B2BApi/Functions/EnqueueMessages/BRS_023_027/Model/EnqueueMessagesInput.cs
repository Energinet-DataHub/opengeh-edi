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

using System.Collections.Immutable;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Model;

/// <summary>
/// An immutable input to start the enqueue messages activity.
/// </summary>
public sealed record EnqueueMessagesInput(
    Guid CalculationId,
    Guid EventId,
    ImmutableDictionary<string, ActorNumber> GridAreaOwners);

public sealed record EnqueueMessagesForActorInput(
    Guid CalculationId,
    Guid EventId,
    ImmutableDictionary<string, ActorNumber> GridAreaOwners,
    string Actor);
