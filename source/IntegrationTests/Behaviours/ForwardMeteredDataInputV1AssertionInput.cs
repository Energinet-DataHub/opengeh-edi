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
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours;

public record ForwardMeteredDataInputV1AssertionInput(
    string ActorNumber,
    string ActorRole,
    TransactionId TransactionId,
    string? MeteringPointId,
    string? MeteringPointType,
    string? ProductNumber,
    string? MeasureUnit,
    Instant RegistrationDateTime,
    Resolution? Resolution,
    Instant StartDateTime,
    Instant? EndDateTime,
    string? GridAccessProviderNumber,
    IReadOnlyCollection<string>? DelegatedGridAreas,
    IReadOnlyCollection<ForwardMeteredDataInputV1.EnergyObservation> EnergyObservations);
