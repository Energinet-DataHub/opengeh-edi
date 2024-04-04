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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.BuildingBlocks.Models;

public class DataHubTypeWithUnknownTests
{
    /// <summary>
    /// Get all types that inherit from DataHubTypeWithUnknown base classes (a list of <see cref="DataHubTypeWithUnknown{T}"/>))
    /// </summary>
    public static IEnumerable<object[]> GetAllDataHubTypeWithUnknown()
    {
        var dataHubTypeWithUnknownTypes = Assembly.GetAssembly(typeof(DataHubTypeWithUnknown<>))!
            .GetTypes()
            .Where(t => t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(DataHubTypeWithUnknown<>))
            .Select(type => new object[]
            {
                type.BaseType!, // Get the DataHubTypeWithUnknown<T> base type instead of the implementation
            })
            .ToList();

        return dataHubTypeWithUnknownTypes;
    }

    [Theory]
    [MemberData(nameof(GetAllDataHubTypeWithUnknown))]
    public void Ensure_all_can_be_created_as_unknown(Type dataHubTypeWithUnknown)
    {
        ArgumentNullException.ThrowIfNull(dataHubTypeWithUnknown);

        // Arrange
        var unknownCode = "UNKNOWN-CODE";
        var fromCodeOrUnknownMethod = dataHubTypeWithUnknown.GetMethod("FromCodeOrUnknown", BindingFlags.Public | BindingFlags.Static);

        // Act
        var act = () => fromCodeOrUnknownMethod!.Invoke(null, new object[] { unknownCode });

        // Assert
        using var scope = new AssertionScope();
        var result = act.Should().NotThrow().Subject;
        result.Should().NotBeNull();
        dataHubTypeWithUnknown.GetProperty("IsUnknown")!.GetValue(result).Should().Be(true);
        dataHubTypeWithUnknown.GetProperty("Name")!.GetValue(result).Should().Be(unknownCode);
        dataHubTypeWithUnknown.GetProperty("Code")!.GetValue(result).Should().Be(unknownCode);
    }
}
