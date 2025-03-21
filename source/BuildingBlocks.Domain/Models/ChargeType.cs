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

using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ChargeType : DataHubType<ChargeType>
{
    public static readonly ChargeType Subscription = new(PMTypes.ChargeType.Subscription.Name, "D01");
    public static readonly ChargeType Fee = new(PMTypes.ChargeType.Fee.Name, "D02");
    public static readonly ChargeType Tariff = new(PMTypes.ChargeType.Tariff.Name, "D03");

    public ChargeType(string name, string code)
        : base(name, code)
    {
    }
}
