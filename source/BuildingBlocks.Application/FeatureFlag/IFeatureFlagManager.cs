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

namespace BuildingBlocks.Application.FeatureFlag;

/// <summary>
/// Manage feature flags in the application. If using <see cref="MicrosoftFeatureFlagManager"/> then the feature flags are managed through the app configuration, and the name of a feature flag configuration must be prefixed with "FeatureManagement__", ie. "FeatureManagement__UseMonthlyAmountPerChargeResultProduced"
/// The "Feature Flags in EDI" documentation page in confluence should be kept up-to-date: https://energinet.atlassian.net/wiki/spaces/D3/pages/677412898/Feature+Flags+in+EDI
/// </summary>
public interface IFeatureFlagManager
{
    /// <summary>
    /// A Feature Flag example
    /// </summary>
    Task<bool> UseExampleFeatureFlagAsync();

    /// <summary>
    /// Whether to allow handling MonthlyAmountPerChargeResultProduced events
    /// </summary>
    Task<bool> UseMonthlyAmountPerChargeResultProducedAsync();

    /// <summary>
    /// Whether to allow handling AmountPerChargeResultProduced events
    /// </summary>
    Task<bool> UseAmountPerChargeResultProducedAsync();

    /// <summary>
    /// Whether to allow handling WholesaleSettlement Request
    /// </summary>
    Task<bool> UseRequestWholesaleSettlementReceiverAsync();

    /// <summary>
    /// Whether to allow message delegation for actors.
    /// </summary>
    Task<bool> UseMessageDelegationAsync();

    /// <summary>
    /// Whether to allow actors to peek messages.
    /// </summary>
    Task<bool> UsePeekMessagesAsync();

    /// <summary>
    /// Whether to allow actors to request messages.
    /// </summary>
    Task<bool> UseRequestMessagesAsync();

    /// <summary>
    /// Whether to allow handling EnergyResultProducedV2 events.
    /// </summary>
    Task<bool> UseEnergyResultProducedAsync();

    /// <summary>
    /// Whether to allow handling TotalMonthlyAmountResultProduced events.
    /// </summary>
    Task<bool> UseTotalMonthlyAmountResultProducedAsync();
}
