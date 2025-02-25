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

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;

/// <summary>
/// This class contains the metering point types that are used in the delta tables described here:
/// https://energinet.atlassian.net/wiki/spaces/D3/pages/1014202369/Wholesale+Results
/// </summary>
public static class DeltaTableMeteringPointType
{
    public const string Consumption = "consumption";
    public const string Production = "production";
    public const string Exchange = "exchange";
    public const string VeProduction = "ve_production";
    public const string NetProduction = "net_production";
    public const string SupplyToGrid = "supply_to_grid";
    public const string ConsumptionFromGrid = "consumption_from_grid";
    public const string WholesaleServicesInformation = "wholesale_services_information";
    public const string OwnProduction = "own_production";
    public const string NetFromGrid = "net_from_grid";
    public const string NetToGrid = "net_to_grid";
    public const string TotalConsumption = "total_consumption";
    public const string ElectricalHeating = "electrical_heating";
    public const string NetConsumption = "net_consumption";
    public const string CapacitySettlement = "capacity_settlement";
}
