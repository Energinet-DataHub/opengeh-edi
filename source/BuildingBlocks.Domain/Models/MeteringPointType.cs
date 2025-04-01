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
    public static readonly MeteringPointType VeProduction = new(PMTypes.MeteringPointType.VeProduction.Name, "D01");
    public static readonly MeteringPointType Analysis = new(PMTypes.MeteringPointType.Analysis.Name, "D02");
    public static readonly MeteringPointType SurplusProductionGroup6 = new(PMTypes.MeteringPointType.SurplusProductionGroup6.Name, "D04");
    public static readonly MeteringPointType NetProduction = new(PMTypes.MeteringPointType.NetProduction.Name, "D05");
    public static readonly MeteringPointType SupplyToGrid = new(PMTypes.MeteringPointType.SupplyToGrid.Name, "D06");
    public static readonly MeteringPointType ConsumptionFromGrid = new(PMTypes.MeteringPointType.ConsumptionFromGrid.Name, "D07");
    public static readonly MeteringPointType WholesaleServicesInformation = new(PMTypes.MeteringPointType.WholesaleServicesInformation.Name, "D08");
    public static readonly MeteringPointType OwnProduction = new(PMTypes.MeteringPointType.OwnProduction.Name, "D09");
    public static readonly MeteringPointType NetFromGrid = new(PMTypes.MeteringPointType.NetFromGrid.Name, "D10");
    public static readonly MeteringPointType NetToGrid = new(PMTypes.MeteringPointType.NetToGrid.Name, "D11");
    public static readonly MeteringPointType TotalConsumption = new(PMTypes.MeteringPointType.TotalConsumption.Name, "D12");
    public static readonly MeteringPointType ElectricalHeating = new(PMTypes.MeteringPointType.ElectricalHeating.Name, "D14");
    public static readonly MeteringPointType NetConsumption = new(PMTypes.MeteringPointType.NetConsumption.Name, "D15");
    public static readonly MeteringPointType OtherConsumption = new(PMTypes.MeteringPointType.OtherConsumption.Name, "D17");
    public static readonly MeteringPointType OtherProduction = new(PMTypes.MeteringPointType.OtherProduction.Name, "D18");
    public static readonly MeteringPointType CapacitySettlement = new(PMTypes.MeteringPointType.CapacitySettlement.Name, "D19");
    public static readonly MeteringPointType ExchangeReactiveEnergy = new(PMTypes.MeteringPointType.ExchangeReactiveEnergy.Name, "D20");
    public static readonly MeteringPointType CollectiveNetProduction = new(PMTypes.MeteringPointType.CollectiveNetProduction.Name, "D21");
    public static readonly MeteringPointType CollectiveNetConsumption = new(PMTypes.MeteringPointType.CollectiveNetConsumption.Name, "D22");
    // public static readonly MeteringPointType InternalUse = new(PMTypes.MeteringPointType.InternalUse.Name, "D99");

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
