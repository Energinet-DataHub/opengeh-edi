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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.DeltaTableMappers;

public class ChargeTypeMapperTests
{
    public static IEnumerable<object[]> GetAllChargeTypes()
    {
        var fields =
            typeof(DeltaTableChargeType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => new[] { f.GetValue(null)! });
    }

    public static IEnumerable<object?[]> GetAllChargeTypesIncludingNull()
    {
        var fields =
            typeof(DeltaTableChargeType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => new[] { f.GetValue(null)! });
    }

    [Theory]
    [MemberData(nameof(GetAllChargeTypes))]
    public void Ensure_all_ChargeTypes(string chargeType)
    {
        // Act
        var act = () => ChargeTypeMapper.FromDeltaTableValue(chargeType);

        // Assert
        var result = act.Should().NotThrow().Subject;
        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(GetAllChargeTypesIncludingNull))]
    public void Ensure_all_ChargeTypes_as_nullable(string? chargeType)
    {
        // Act
        var act = () => ChargeTypeMapper.FromDeltaTableValueAsNullable(chargeType);

        // Assert
        act.Should().NotThrow();
    }
}
