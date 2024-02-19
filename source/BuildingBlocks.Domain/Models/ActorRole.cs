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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

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

    private ActorRole(string name, string code)
        : base(name)
    {
        Code = code;
    }

    public string Code { get; }

    public static ActorRole FromCode(string code)
    {
        var matchingItem = GetAll<ActorRole>().FirstOrDefault(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return matchingItem ?? throw new InvalidOperationException($"'{code}' is not a valid code in {typeof(ActorRole)}");
    }

    public override string ToString()
    {
        return Name;
    }
}
