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
    private static readonly Dictionary<string, Models.ActorRole> ActorRoleMappings = new()
    {
        { ActorRole.MeteringPointAdministrator.Code, Models.ActorRole.MeteringPointAdministrator },
        { ActorRole.EnergySupplier.Code, Models.ActorRole.EnergySupplier },
        { ActorRole.GridAccessProvider.Code, Models.ActorRole.GridAccessProvider },
        { ActorRole.MeteredDataAdministrator.Code, Models.ActorRole.MeteredDataAdministrator },
        { ActorRole.MeteredDataResponsible.Code, Models.ActorRole.MeteredDataResponsible },
        { ActorRole.BalanceResponsibleParty.Code, Models.ActorRole.BalanceResponsibleParty },
        { ActorRole.ImbalanceSettlementResponsible.Code, Models.ActorRole.ImbalanceSettlementResponsible },
        { ActorRole.SystemOperator.Code, Models.ActorRole.SystemOperator },
        { ActorRole.DanishEnergyAgency.Code, Models.ActorRole.DanishEnergyAgency },
        { ActorRole.Delegated.Code, Models.ActorRole.Delegated },
        { ActorRole.DataHubAdministrator.Code, Models.ActorRole.DataHubAdministrator },
    };

    public static Models.ActorRole? ToActorRole(string? actorRoleCode)
    {
        return actorRoleCode == null ? null : ActorRoleMappings[actorRoleCode];
    }

    public static string? ToActorRoleCode(Models.ActorRole? actorRole)
    {
        return ActorRoleMappings.FirstOrDefault(x => x.Value == actorRole).Key;
    }
}
