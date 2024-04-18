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
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviors;

public static class MetaTests
{
    private static readonly string[] _excludedTestFolders =
    {
        "Energinet.DataHub.EDI.IntegrationTests.B2BApi",
        "Energinet.DataHub.EDI.IntegrationTests.Application",
        "Energinet.DataHub.EDI.IntegrationTests.Infrastructure",
    };

    [Fact]
    public static void Given_TestNames_When_CheckingConvention_Then_AllSatisfies()
    {
        // Arrange
        var needsToBeOfForm_Given_xx_When_yy_Then_zz = @"^(((Given)|(AndGiven))_[^_]+_)?When_[^_]+_Then_[^_]+$";

        var allTestNames = Assembly.GetAssembly(typeof(MetaTests))?
            .GetTypes()
            .Where(
                type =>
                    type.IsClass
                    && type.Namespace != null
                    && !_excludedTestFolders.Any(f => type.Namespace.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(type => type.GetMethods())
            .Where(IsXunitTest)
            .Select(m => m.Name)
            .ToArray();

        // Assert
        allTestNames.Should().NotBeNull();
        allTestNames.Should().AllSatisfy(name => name.Should().MatchRegex(needsToBeOfForm_Given_xx_When_yy_Then_zz));
    }

    [Fact]
    [ExcludeFromNameConventionCheck]
    public static void ThisDoesNotSatisfyTheNamingConventionFact()
    {
        return;
    }

    [Theory]
    [InlineData("test")]
    [ExcludeFromNameConventionCheck]
    public static void ThisDoesNotSatisfyTheNamingConventionTheory(string test)
    {
        test.Should().Be("test");
    }

    private static bool IsXunitTest(MethodInfo type)
    {
        return type.CustomAttributes.Any(t => t.AttributeType.Namespace == "Xunit")
            && type.CustomAttributes.All(t => t.AttributeType.Name != nameof(ExcludeFromNameConventionCheckAttribute));
    }
}
