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
    private static readonly Dictionary<Models.ActorRole, string> ActorRoleMappings = new()
    {
        { Models.ActorRole.MeteringPointAdministrator, ActorRole.MeteringPointAdministrator.Code },
        { Models.ActorRole.EnergySupplier, ActorRole.EnergySupplier.Code },
        { Models.ActorRole.GridAccessProvider, ActorRole.GridAccessProvider.Code },
        { Models.ActorRole.MeteredDataAdministrator, ActorRole.MeteredDataAdministrator.Code },
        { Models.ActorRole.MeteredDataResponsible, ActorRole.MeteredDataResponsible.Code },
        { Models.ActorRole.BalanceResponsibleParty, ActorRole.BalanceResponsibleParty.Code },
        { Models.ActorRole.ImbalanceSettlementResponsible, ActorRole.ImbalanceSettlementResponsible.Code },
        { Models.ActorRole.SystemOperator, ActorRole.SystemOperator.Code },
        { Models.ActorRole.DanishEnergyAgency, ActorRole.DanishEnergyAgency.Code },
        { Models.ActorRole.Delegated, ActorRole.Delegated.Code },
        { Models.ActorRole.DataHubAdministrator, ActorRole.DataHubAdministrator.Code },
    };

    public static Models.ActorRole ToActorRole(string actorRoleCode)
    {
        if (ActorRoleMappings.ContainsValue(actorRoleCode))
        {
            return ActorRoleMappings.First(x => x.Value == actorRoleCode).Key;
        }

        throw new NotSupportedException($"Actor role not supported: {actorRoleCode}");
    }
}
