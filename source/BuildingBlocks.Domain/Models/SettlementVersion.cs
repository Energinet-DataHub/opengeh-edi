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

using System.Text.Json.Serialization;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class SettlementVersion : DataHubTypeWithUnused<SettlementVersion>
{
    public static readonly SettlementVersion FirstCorrection = new(PMTypes.SettlementVersion.FirstCorrection.Name, "D01");
    public static readonly SettlementVersion SecondCorrection = new(PMTypes.SettlementVersion.SecondCorrection.Name, "D02");
    public static readonly SettlementVersion ThirdCorrection = new(PMTypes.SettlementVersion.ThirdCorrection.Name, "D03");

    [JsonConstructor]
    private SettlementVersion(string name, string code, bool isUnused = false)
        : base(name, code, isUnused)
    {
    }
}
