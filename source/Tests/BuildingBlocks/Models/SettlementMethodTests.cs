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

public class SettlementMethodTests
{
    public static IEnumerable<SettlementMethod> GetAllSettlementMethods()
    {
        var fields =
            typeof(SettlementMethod).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<SettlementMethod>();
    }

    [Fact]
    public void Ensure_all_SettlementMethods()
    {
        var settlementMethods = new List<(SettlementMethod ExpectedValue, string Name, string Code)>()
        {
            (SettlementMethod.Flex, "Flex", "D01"), (SettlementMethod.NonProfiled, "NonProfiled", "E02"),
        };

        using var scope = new AssertionScope();
        foreach (var test in settlementMethods)
        {
            SettlementMethod.FromName(test.Name).Should().Be(test.ExpectedValue);
            SettlementMethod.FromCode(test.Code).Should().Be(test.ExpectedValue);
        }

        settlementMethods.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllSettlementMethods());
    }
}
