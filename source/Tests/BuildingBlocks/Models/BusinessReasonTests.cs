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

public class BusinessReasonTests
{
    public static IEnumerable<BusinessReason> GetAllBusinessReasons()
    {
        var fields =
            typeof(BusinessReason).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null))
            .Cast<BusinessReason>()
            .Where(
                br => !(
                    br.Name.Length == 3
                    && (br.Name.Substring(0, 1) == "D"
                        || br.Name.Substring(0, 1) == "E"
                        || br.Name.Substring(0, 1) == "A")));
    }

    [Fact]
    public void Ensure_all_businessReasons()
    {
        var businessReasons = new List<(BusinessReason ExpectedValue, string Name, string Code)>()
        {
            (BusinessReason.BalanceFixing, "BalanceFixing", "D04"),
            (BusinessReason.Correction, "Correction", "D32"),
            (BusinessReason.MoveIn, "MoveIn", "E65"),
            (BusinessReason.PreliminaryAggregation, "PreliminaryAggregation", "D03"),
            (BusinessReason.WholesaleFixing, "WholesaleFixing", "D05"),
        };

        using var scope = new AssertionScope();
        foreach (var test in businessReasons)
        {
            BusinessReason.FromName(test.Name).Should().Be(test.ExpectedValue);
            BusinessReason.FromCode(test.Code).Should().Be(test.ExpectedValue);
        }

        businessReasons.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllBusinessReasons());
    }
}
