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

using System.Collections.Generic;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public static class EnergyResultProducedProcessorExtensions
{
    public static IReadOnlyCollection<EnergyResultProducedV2.Types.TimeSeriesType> SupportedTimeSeriesTypes() =>
        new List<EnergyResultProducedV2.Types.TimeSeriesType>
        {
            EnergyResultProducedV2.Types.TimeSeriesType.Production,
            EnergyResultProducedV2.Types.TimeSeriesType.FlexConsumption,
            EnergyResultProducedV2.Types.TimeSeriesType.NonProfiledConsumption,
            EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa,
            EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption,
        };
}
