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
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal static class ProcessDelegationEventBuilder
{
#pragma warning disable CA1802
    private static readonly ActorNumber _delegatedByActorNumber = ActorNumber.Create("1234567890123");
    private static readonly EicFunction _delegatedByActorRole = EicFunction.EnergySupplier;
    private static readonly ActorNumber _delegatedToActorNumber = ActorNumber.Create("1234567890124");
    private static readonly EicFunction _delegatedToActorRole = EicFunction.EnergySupplier;
    private static readonly DelegatedProcess _delegatedProcess = DelegatedProcess.ProcessReceiveEnergyResults;
    private static readonly string _gridAreaCode = "804";
    private static readonly Instant _startsAt = Instant.FromUtc(2023, 8, 1, 0, 0);
    private static readonly Instant _stopsAt = Instant.FromUtc(2023, 12, 1, 0, 0);
#pragma warning restore CA1802

    internal static ProcessDelegationConfigured Build()
    {
        return new ProcessDelegationConfigured()
        {
            DelegatedByActorNumber = _delegatedByActorNumber.Value,
            DelegatedByActorRole = _delegatedByActorRole,
            DelegatedToActorNumber = _delegatedToActorNumber.Value,
            DelegatedToActorRole = _delegatedToActorRole,
            GridAreaCode = _gridAreaCode,
            StartsAt = _startsAt.ToTimestamp(),
            StopsAt = _stopsAt.ToTimestamp(),
            Process = _delegatedProcess,
        };
    }
}
