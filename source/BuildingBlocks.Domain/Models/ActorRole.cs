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

using System;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ActorRole : EnumerationType
{
    public static readonly ActorRole MeteringPointAdministrator = new("MeteringPointAdministrator", "DDZ");
    public static readonly ActorRole EnergySupplier = new("EnergySupplier", "DDQ");

    // A grid operator has two roles.
    // GridOperator (DDM) when creating a new metering point
    public static readonly ActorRole GridOperator = new("GridOperator", "DDM");
    public static readonly ActorRole MeteredDataAdministrator = new("MeteredDataAdministrator", "DGL");

    // A grid operator has two roles.
    // MeteredDataResponsible (MDR) when requesting data from DataHub
    public static readonly ActorRole MeteredDataResponsible = new("MeteredDataResponsible", "MDR");
    public static readonly ActorRole BalanceResponsibleParty = new("BalanceResponsibleParty", "DDK");

    public static readonly ActorRole ImbalanceSettlementResponsible = new("ImbalanceSettlementResponsible", "DDX");
    public static readonly ActorRole SystemOperator = new("SystemOperator", "EZ");
    public static readonly ActorRole DanishEnergyAgency = new("DanishEnergyAgency", "STS");
    public static readonly ActorRole Delegated = new("Delegated", "DEL");

    private ActorRole(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static ActorRole FromCode(string code)
    {
        return GetAll<ActorRole>().FirstOrDefault(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{code} is not a valid {typeof(ActorRole)} {nameof(code)}");
    }

    public static ActorRole FromName(string name)
    {
        return GetAll<ActorRole>().FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(ActorRole)} {nameof(name)}");
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
        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack && Equals(MeteredDataResponsible))
            return GridOperator;

        return this;
    }
}
