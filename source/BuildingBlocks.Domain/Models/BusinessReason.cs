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

using System.Text.Json.Serialization;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public sealed class BusinessReason : DataHubTypeWithUnused<BusinessReason>
{
    public static readonly BusinessReason MoveIn = new(PMTypes.BusinessReason.MoveIn.Name, "E65");
    public static readonly BusinessReason BalanceFixing = new(PMTypes.BusinessReason.BalanceFixing.Name, "D04");
    public static readonly BusinessReason PreliminaryAggregation = new(PMTypes.BusinessReason.PreliminaryAggregation.Name, "D03");
    public static readonly BusinessReason WholesaleFixing = new(PMTypes.BusinessReason.WholesaleFixing.Name, "D05"); // Engrosfiksering
    public static readonly BusinessReason Correction = new(PMTypes.BusinessReason.Correction.Name, "D32");
    public static readonly BusinessReason PeriodicMetering = new(PMTypes.BusinessReason.PeriodicMetering.Name, "E23");
    public static readonly BusinessReason PeriodicFlexMetering = new(PMTypes.BusinessReason.PeriodicFlexMetering.Name, "D42");

    [JsonConstructor]
    private BusinessReason(string name, string code, bool isUnused = false)
     : base(name, code, isUnused) { }

    public ProcessManager.Components.Abstractions.ValueObjects.BusinessReason ToProcessManagerBusinessReason()
    {
        return ProcessManager.Components.Abstractions.ValueObjects.BusinessReason.FromName(Name);
    }
}
