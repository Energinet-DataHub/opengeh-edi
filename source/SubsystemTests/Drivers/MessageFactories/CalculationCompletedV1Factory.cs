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

using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

internal static class CalculationCompletedV1Factory
{
    public static CalculationCompletedV1 CreateCalculationCompleted(
        Guid calculationId,
        CalculationCompletedV1.Types.CalculationType calculationType)
    {
        var calculationCompletedV1 = new CalculationCompletedV1
        {
            CalculationId = calculationId.ToString(),
            CalculationType = calculationType,
            CalculationVersion = 0,
            InstanceId = Guid.Parse("00000000-0000-0000-0000-000000000001").ToString(),
        };

        return calculationCompletedV1;
    }
}
