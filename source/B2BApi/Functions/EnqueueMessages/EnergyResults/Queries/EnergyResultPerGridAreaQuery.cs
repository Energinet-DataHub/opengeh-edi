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

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.Queries;

/// <summary>
/// The query we must perform on the 'Energy Result Per Grid Area' view.
///
/// Keep the code updated with regards to the documentation in confluence in a way
/// that it is easy to compare (e.g. order of columns).
/// See confluence: https://energinet.atlassian.net/wiki/spaces/D3/pages/849805314/Calculation+Result+Model#Energy-Result-Points-Per-Grid-Area
/// </summary>
public class EnergyResultPerGridAreaQuery(Guid calculationId)
    : EnergyResultQueryBase(calculationId)
{
    public override string DatabaseName => "wholesale_edi_results";

    public override string DataObjectName => "energy_result_points_per_ga_v1";

    public override string[] SqlColumnNames => [
        EnergyResultViewColumnNames.CalculationId,
        EnergyResultViewColumnNames.CalculationType,
        EnergyResultViewColumnNames.CalculationPeriodStart,
        EnergyResultViewColumnNames.CalculationPeriodEnd,
        EnergyResultViewColumnNames.CalculationVersion,
        EnergyResultViewColumnNames.ResultId,
        EnergyResultViewColumnNames.GridAreaCode,
        EnergyResultViewColumnNames.MeteringPointType,
        EnergyResultViewColumnNames.SettlementMethod,
        EnergyResultViewColumnNames.Resolution,
        EnergyResultViewColumnNames.Time,
        EnergyResultViewColumnNames.Quantity,
        EnergyResultViewColumnNames.QuantityUnit,
        EnergyResultViewColumnNames.QuantityQualities];
}
