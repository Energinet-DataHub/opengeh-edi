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

public class ResolutionTests
{
    public static IEnumerable<Resolution> GetAllResolutions()
    {
        var fields =
            typeof(Resolution).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<Resolution>();
    }

    [Fact]
    public void Ensure_all_Resolutions()
    {
        var resolutions = new List<(Resolution ExpectedValue, string Name, string Code)>()
        {
            (Resolution.QuarterHourly, "QuarterHourly", "PT15M"),
            (Resolution.Hourly, "Hourly", "PT1H"),
            (Resolution.Daily, "Daily", "P1D"),
            (Resolution.Monthly, "Monthly", "P1M"),
        };

        using var scope = new AssertionScope();
        foreach (var test in resolutions)
        {
            Resolution.FromName(test.Name).Should().Be(test.ExpectedValue);
            Resolution.FromCode(test.Code).Should().Be(test.ExpectedValue);
        }

        resolutions.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllResolutions());
    }
}
