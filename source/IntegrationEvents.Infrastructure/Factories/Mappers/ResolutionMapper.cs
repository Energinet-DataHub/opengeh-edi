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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class ResolutionMapper
{
    public static Resolution Map(EnergyResultProducedV2.Types.Resolution resolution)
    {
        return resolution switch
        {
            EnergyResultProducedV2.Types.Resolution.Quarter => Resolution.QuarterHourly,
            // EnergyResultProducedV2.Types.Resolution.Hourly => Resolution.Hourly,
            EnergyResultProducedV2.Types.Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unknown resolution from Wholesale"),
        };
    }

    public static Resolution Map(AmountPerChargeResultProducedV1.Types.Resolution resolution)
    {
        return resolution switch
        {
            AmountPerChargeResultProducedV1.Types.Resolution.Day => Resolution.Daily,
            AmountPerChargeResultProducedV1.Types.Resolution.Hour => Resolution.Hourly,
            AmountPerChargeResultProducedV1.Types.Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unknown resolution from Wholesale"),
        };
    }
}
