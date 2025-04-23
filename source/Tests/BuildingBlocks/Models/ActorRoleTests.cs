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
        var fields = typeof(ActorRole).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<ActorRole>();
    }

    [Fact]
    public void Ensure_all_ActorRoles()
    {
        var actorRole = new List<(ActorRole ExpectedValue, string Name, string Code, byte DatabaseValue)>()
        {
            (ActorRole.MeteredDataResponsible, "MeteredDataResponsible", "MDR", 5),
            (ActorRole.MeteredDataAdministrator, "MeteredDataAdministrator", "DGL", 4),
            (ActorRole.GridAccessProvider, "GridAccessProvider", "DDM", 3),
            (ActorRole.BalanceResponsibleParty, "BalanceResponsibleParty", "DDK", 6),
            (ActorRole.EnergySupplier, "EnergySupplier", "DDQ", 2),
            (ActorRole.MeteringPointAdministrator, "MeteringPointAdministrator", "DDZ", 1),
            (ActorRole.ImbalanceSettlementResponsible, "ImbalanceSettlementResponsible", "DDX", 7),
            (ActorRole.SystemOperator, "SystemOperator", "EZ", 8),
            (ActorRole.DanishEnergyAgency, "DanishEnergyAgency", "STS", 9),
            (ActorRole.Delegated, "Delegated", "DEL", 10),
            (ActorRole.DataHubAdministrator, "DataHubAdministrator", string.Empty, 11),
        };

        using var scope = new AssertionScope();
        foreach (var test in actorRole)
        {
            ActorRole.FromName(test.Name).Should().Be(test.ExpectedValue);
            ActorRole.FromCode(test.Code).Should().Be(test.ExpectedValue);
            ActorRole.FromDatabaseValue(test.DatabaseValue).Should().Be(test.ExpectedValue);
        }

        actorRole.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllActorRoles());
    }
}
