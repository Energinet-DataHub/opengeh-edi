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

using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ActorRole : DataHubType<ActorRole>
{
    public static readonly ActorRole MeteringPointAdministrator = new(DataHubNames.ActorRole.MeteringPointAdministrator, "DDZ");
    public static readonly ActorRole EnergySupplier = new(DataHubNames.ActorRole.EnergySupplier, "DDQ");

    // A grid operator has two roles.
    // GridOperator (DDM) when creating a new metering point
    public static readonly ActorRole GridAccessProvider = new(DataHubNames.ActorRole.GridAccessProvider, "DDM");
    public static readonly ActorRole MeteredDataAdministrator = new(DataHubNames.ActorRole.MeteredDataAdministrator, "DGL");

    // A grid operator has two roles.
    // MeteredDataResponsible (MDR) when requesting data from DataHub
    public static readonly ActorRole MeteredDataResponsible = new(DataHubNames.ActorRole.MeteredDataResponsible, "MDR");
    public static readonly ActorRole BalanceResponsibleParty = new(DataHubNames.ActorRole.BalanceResponsibleParty, "DDK");

    public static readonly ActorRole ImbalanceSettlementResponsible = new(DataHubNames.ActorRole.ImbalanceSettlementResponsible, "DDX");
    public static readonly ActorRole SystemOperator = new(DataHubNames.ActorRole.SystemOperator, "EZ");
    public static readonly ActorRole DanishEnergyAgency = new(DataHubNames.ActorRole.DanishEnergyAgency, "STS");
    public static readonly ActorRole Delegated = new(DataHubNames.ActorRole.Delegated, "DEL");

    // DataHubAdministrator is a special role that is used to indicate that the user has special permissions.
    public static readonly ActorRole DataHubAdministrator = new(DataHubNames.ActorRole.DataHubAdministrator, string.Empty);

    [JsonConstructor]
    private ActorRole(string name, string code)
        : base(name, code)
    {
    }

    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// The ActorRole for a ActorMessageQueue. This is implemented to support the "hack" where
    ///     a MeteredDataResponsible uses the GridOperator queue
    /// Is used when peeking, dequeuing and enqueuing
    /// </summary>
    public ActorRole ForActorMessageQueue()
    {
        return HackForMeteredDataResponsible();
    }

    /// <summary>
    /// The ActorRole for a ActorMessageDelegation. This is implemented to support the "hack" where
    ///     a MeteredDataResponsible uses the GridOperator
    /// </summary>
    public ActorRole ForActorMessageDelegation()
    {
        return HackForMeteredDataResponsible();
    }

    private ActorRole HackForMeteredDataResponsible()
    {
        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack && Equals(MeteredDataResponsible))
            return GridAccessProvider;

        return this;
    }
}
