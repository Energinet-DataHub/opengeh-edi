﻿// Copyright 2020 Energinet DataHub A/S
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

public class SettlementVersionTests
{
    public static IEnumerable<SettlementVersion> GetAllSettlementVersions()
    {
        var fields =
            typeof(SettlementVersion).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        return fields.Select(f => f.GetValue(null)).Cast<SettlementVersion>();
    }

    [Fact]
    public void Ensure_all_SettlementVersions()
    {
        var settlementVersions = new List<(SettlementVersion ExpectedValue, string Name, string Code)>()
        {
            (SettlementVersion.FirstCorrection, "FirstCorrection", "D01"),
            (SettlementVersion.SecondCorrection, "SecondCorrection", "D02"),
            (SettlementVersion.ThirdCorrection, "ThirdCorrection", "D03"),
            (SettlementVersion.FourthCorrection, "FourthCorrection", "D04"),
            (SettlementVersion.FifthCorrection, "FifthCorrection", "D05"),
            (SettlementVersion.SixthCorrection, "SixthCorrection", "D06"),
            (SettlementVersion.SeventhCorrection, "SeventhCorrection", "D07"),
            (SettlementVersion.EighthCorrection, "EighthCorrection", "D08"),
            (SettlementVersion.NinthCorrection, "NinthCorrection", "D09"),
            (SettlementVersion.TenthCorrection, "TenthCorrection", "D10"),
        };

        using var scope = new AssertionScope();
        foreach (var test in settlementVersions)
        {
            SettlementVersion.FromName(test.Name).Should().Be(test.ExpectedValue);
            SettlementVersion.FromCode(test.Code).Should().Be(test.ExpectedValue);
        }

        settlementVersions.Select(c => c.ExpectedValue).Should().BeEquivalentTo(GetAllSettlementVersions());
    }
}
