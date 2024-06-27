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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.DeltaTableConstants;

public class DeltaTableChargeTypeTests
{
    public static IEnumerable<string> GetAllChargeTypes()
    {
        var fields =
            typeof(DeltaTableChargeType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)!.ToString()!);
    }

    [Fact]
    public void Ensure_all_DeltaTableChargeTypes()
    {
        // Arrange
        var expectedDeltaTableChargeTypes = new List<string>()
        {
            "subscription",
            "fee",
            "tariff",
        };

        // Act
        expectedDeltaTableChargeTypes.Should().BeEquivalentTo(GetAllChargeTypes());
    }
}
