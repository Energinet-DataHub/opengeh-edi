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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class ProcessType : EnumerationType
{
    public static readonly ProcessType RequestedEnergyResults = new(nameof(RequestedEnergyResults));
    public static readonly ProcessType CalculatedEnergyResults = new(nameof(CalculatedEnergyResults));
    public static readonly ProcessType RequestedWholesaleResults = new(nameof(RequestedWholesaleResults));
    public static readonly ProcessType CalculatedWholesaleResults = new(nameof(CalculatedWholesaleResults));

    private ProcessType(string name)
        : base(name)
    {
    }

    public static ProcessType FromName(string name)
    {
        return GetAll<ProcessType>().FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(ProcessType)} {nameof(name)}");
    }
}
