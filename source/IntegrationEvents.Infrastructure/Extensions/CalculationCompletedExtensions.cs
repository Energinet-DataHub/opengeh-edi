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

using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Extensions;

public static class CalculationCompletedExtensions
{
    private enum CalculationType
    {
        BalanceFixing,
        WholesaleFixing,
        Other,
    }

    public static Task<bool> IsHandledByCalculationCompletedEventAsync(
        this CalculationCompletedV1.Types.CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        return IsHandledByCalculationCompletedEventAsync(
            calculationType switch {
                CalculationCompletedV1.Types.CalculationType.BalanceFixing => CalculationType.BalanceFixing,
                CalculationCompletedV1.Types.CalculationType.WholesaleFixing => CalculationType.WholesaleFixing,
                _ => CalculationType.Other,
            },
            featureFlagManager);
    }

    public static Task<bool> IsHandledByCalculationCompletedEventAsync(
        this EnergyResultProducedV2.Types.CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        return IsHandledByCalculationCompletedEventAsync(
            calculationType switch {
                EnergyResultProducedV2.Types.CalculationType.BalanceFixing => CalculationType.BalanceFixing,
                EnergyResultProducedV2.Types.CalculationType.WholesaleFixing => CalculationType.WholesaleFixing,
                _ => CalculationType.Other,
            },
            featureFlagManager);
    }

    public static Task<bool> IsHandledByCalculationCompletedEventAsync(
        this AmountPerChargeResultProducedV1.Types.CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        return IsHandledByCalculationCompletedEventAsync(
            calculationType switch {
                AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => CalculationType.WholesaleFixing,
                _ => CalculationType.Other,
            },
            featureFlagManager);
    }

    public static Task<bool> IsHandledByCalculationCompletedEventAsync(
        this MonthlyAmountPerChargeResultProducedV1.Types.CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        return IsHandledByCalculationCompletedEventAsync(
            calculationType switch {
                MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing => CalculationType.WholesaleFixing,
                _ => CalculationType.Other,
            },
            featureFlagManager);
    }

    public static Task<bool> IsHandledByCalculationCompletedEventAsync(
        this TotalMonthlyAmountResultProducedV1.Types.CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        return IsHandledByCalculationCompletedEventAsync(
            calculationType switch {
                TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing => CalculationType.WholesaleFixing,
                _ => CalculationType.Other,
            },
            featureFlagManager);
    }

    private static async Task<bool> IsHandledByCalculationCompletedEventAsync(
        CalculationType calculationType,
        IFeatureFlagManager featureFlagManager)
    {
        var isCalculationCompletedEnabled = await featureFlagManager.UseCalculationCompletedEventAsync()
            .ConfigureAwait(false);

        // If CalculationCompletedEvent handling is disabled, it doesn't matter what the calculation type is
        if (!isCalculationCompletedEnabled)
            return false;

        return calculationType switch
        {
            CalculationType.BalanceFixing => await featureFlagManager
                .UseCalculationCompletedEventForBalanceFixingAsync()
                .ConfigureAwait(false),

            CalculationType.WholesaleFixing => await featureFlagManager
                .UseCalculationCompletedEventForWholesaleFixingAsync()
                .ConfigureAwait(false),

            _ => false, // CalculationCompletedEvent handling is "opt-in", so it is disabled by default
        };
    }
}
