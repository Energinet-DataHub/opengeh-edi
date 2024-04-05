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

public class DataHubTypeWithUnusedTests
{
    /// <summary>
    /// Get all types that inherit from DataHubTypeWithUnused base classes (a list of <see cref="DataHubTypeWithUnused{T}"/>))
    /// </summary>
    public static IEnumerable<object[]> GetAllDataHubTypeWithUnused()
    {
        var dataHubTypeWithUnusedTypes = Assembly.GetAssembly(typeof(DataHubTypeWithUnused<>))!
            .GetTypes()
            .Where(t => t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(DataHubTypeWithUnused<>))
            .Select(type => new object[]
            {
                type.BaseType!, // Get the DataHubTypeWithUnused<T> base type instead of the implementation
            })
            .ToList();

        return dataHubTypeWithUnusedTypes;
    }

    [Theory]
    [MemberData(nameof(GetAllDataHubTypeWithUnused))]
    public void Ensure_all_can_be_created_as_unused(Type dataHubTypeWithUnused)
    {
        ArgumentNullException.ThrowIfNull(dataHubTypeWithUnused);

        // Arrange
        var unusedCode = "UNUSED-CODE";
        var fromCodeOrUnusedMethod = dataHubTypeWithUnused.GetMethod("FromCodeOrUnused", BindingFlags.Public | BindingFlags.Static);

        // Act
        var act = () => fromCodeOrUnusedMethod!.Invoke(null, new object[] { unusedCode });

        // Assert
        using var scope = new AssertionScope();
        var result = act.Should().NotThrow().Subject;
        result.Should().NotBeNull();
        dataHubTypeWithUnused.GetProperty("IsUnused")!.GetValue(result).Should().Be(true);
        dataHubTypeWithUnused.GetProperty("Name")!.GetValue(result).Should().Be(unusedCode);
        dataHubTypeWithUnused.GetProperty("Code")!.GetValue(result).Should().Be(unusedCode);
    }
}
