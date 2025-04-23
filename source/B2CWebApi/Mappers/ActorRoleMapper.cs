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

namespace Energinet.DataHub.EDI.B2CWebApi.Mappers;

public static class ActorRoleMapper
{
    private static readonly Dictionary<ActorRole, Models.ActorRole> ActorRoleMappings = new()
    {
        { ActorRole.MeteringPointAdministrator, Models.ActorRole.MeteringPointAdministrator },
        { ActorRole.EnergySupplier, Models.ActorRole.EnergySupplier },
        { ActorRole.GridAccessProvider, Models.ActorRole.GridAccessProvider },
        { ActorRole.MeteredDataAdministrator, Models.ActorRole.MeteredDataAdministrator },
        { ActorRole.MeteredDataResponsible, Models.ActorRole.MeteredDataResponsible },
        { ActorRole.BalanceResponsibleParty, Models.ActorRole.BalanceResponsibleParty },
        { ActorRole.ImbalanceSettlementResponsible, Models.ActorRole.ImbalanceSettlementResponsible },
        { ActorRole.SystemOperator, Models.ActorRole.SystemOperator },
        { ActorRole.DanishEnergyAgency, Models.ActorRole.DanishEnergyAgency },
        { ActorRole.Delegated, Models.ActorRole.Delegated },
        { ActorRole.DataHubAdministrator, Models.ActorRole.DataHubAdministrator },
    };

    public static Models.ActorRole? ToActorRoleOrDefault(string? actorRoleCode)
    {
        return actorRoleCode == null ? null : ActorRoleMappings[ActorRole.FromCode(actorRoleCode)];
    }

    public static Models.ActorRole ToActorRole(string actorRoleCode)
    {
        return ActorRoleMappings[ActorRole.FromCode(actorRoleCode)];
    }

    public static string? ToActorRoleCode(Models.ActorRole? actorRole)
    {
        return actorRole == null
            ? null
            : ActorRoleMappings.FirstOrDefault(x => x.Value == actorRole).Key.Code;
    }

    public static ActorRole ToActorRoleDomain(Models.ActorRole actorRole)
    {
        return ActorRoleMappings.First(x => x.Value == actorRole).Key;
    }
}
