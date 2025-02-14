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

[Serializable]
public class MeteringPointType : DataHubType<MeteringPointType>
{
    // Metering point types
    public static readonly MeteringPointType Consumption = new(PMTypes.MeteringPointType.Consumption.Name, "E17");
    public static readonly MeteringPointType Production = new(PMTypes.MeteringPointType.Production.Name, "E18");
    public static readonly MeteringPointType Exchange = new(PMTypes.MeteringPointType.Exchange.Name, "E20");

    // Child metering point types
    public static readonly MeteringPointType VeProduction = new("VeProduction", "D01");
    public static readonly MeteringPointType NetProduction = new("NetProduction", "D05");
    public static readonly MeteringPointType SupplyToGrid = new("SupplyToGrid", "D06");
    public static readonly MeteringPointType ConsumptionFromGrid = new("ConsumptionFromGrid", "D07");
    public static readonly MeteringPointType WholesaleServicesInformation = new("WholesaleServicesInformation", "D08");
    public static readonly MeteringPointType OwnProduction = new("OwnProduction", "D09");
    public static readonly MeteringPointType NetFromGrid = new("NetFromGrid", "D10");
    public static readonly MeteringPointType NetToGrid = new("NetToGrid", "D11");
    public static readonly MeteringPointType TotalConsumption = new("TotalConsumption", "D12");
    public static readonly MeteringPointType ElectricalHeating = new("ElectricalHeating", "D14");
    public static readonly MeteringPointType NetConsumption = new("NetConsumption", "D15");
    public static readonly MeteringPointType CapacitySettlement = new("CapacitySettlement", "D19");

    [JsonConstructor]
    private MeteringPointType(string name, string code)
        : base(name, code)
    {
    }

    public ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType ToProcessManagerMeteringPointType()
    {
        return ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType.FromName(Name);
    }
}
