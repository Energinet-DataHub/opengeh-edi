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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.Models;

public class ActorRoleTests
{
    public static IEnumerable<ActorRole> GetAllActorRoles()
    {
        var fields = typeof(ActorRole)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsLiteral); // Filter out const fields

        return fields.Select(f => f.GetValue(null)).Cast<ActorRole>();
    }

    [Fact]
    public void Ensure_all_ActorRoles()
    {
        var actorRole = new List<(ActorRole ExpectedValue, string Name, string Code)>()
        {
            (ActorRole.MeteredDataResponsible, "MeteredDataResponsible", "MDR"),
            (ActorRole.MeteredDataAdministrator, "MeteredDataAdministrator", "DGL"),
            (ActorRole.GridOperator, "GridOperator", "DDM"),
            (ActorRole.BalanceResponsibleParty, "BalanceResponsibleParty", "DDK"),
            (ActorRole.EnergySupplier, "EnergySupplier", "DDQ"),
            (ActorRole.MeteringPointAdministrator, "MeteringPointAdministrator", "DDZ"),
            (ActorRole.ImbalanceSettlementResponsible, "ImbalanceSettlementResponsible", "DDX"),
            (ActorRole.SystemOperator, "SystemOperator", "EZ"),
            (ActorRole.DanishEnergyAgency, "DanishEnergyAgency", "STS"),
            (ActorRole.Delegated, "Delegated", "DEl"),
        };

        using var scope = new AssertionScope();
        foreach (var test in actorRole)
        {
            ActorRole.FromName(test.Name).Should().Be(test.ExpectedValue);
            ActorRole.FromCode(test.Code).Should().Be(test.ExpectedValue);
        }

        actorRole.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllActorRoles());
    }
}
