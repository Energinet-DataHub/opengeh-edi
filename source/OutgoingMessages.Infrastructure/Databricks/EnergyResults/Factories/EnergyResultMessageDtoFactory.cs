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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;

public class EnergyResultMessageDtoFactory()
{
    public static IReadOnlyCollection<EnergyResultMessagePoint> CreateEnergyResultMessagePoints(IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        ArgumentNullException.ThrowIfNull(timeSeriesPoints);

        return timeSeriesPoints
            .Select(
                (p, index) => new EnergyResultMessagePoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    p.Quantity,
                    CalculatedQuantityQualityMapper.MapForEnergy(p.Qualities),
                    p.TimeUtc.ToString()))
            .ToList()
            .AsReadOnly();
    }
}
