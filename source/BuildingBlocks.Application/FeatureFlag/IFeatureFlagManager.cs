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
/// Manage feature flags in the application. If using <see cref="MicrosoftFeatureFlagManager"/>
/// then the feature flags are managed through the app configuration, and the name
/// of a feature flag configuration must be prefixed with "FeatureManagement__",
/// ie. "FeatureManagement__UseMonthlyAmountPerChargeResultProduced".
/// The "Feature Flags in EDI" documentation page in confluence should be kept
/// up-to-date: https://energinet.atlassian.net/wiki/spaces/D3/pages/677412898/Feature+Flags+in+EDI
/// </summary>
public interface IFeatureFlagManager
{
    /// <summary>
    /// Whether to disallow actors to peek messages.
    /// </summary>
    Task<bool> UsePeekMessagesAsync();

    /// <summary>
    /// Whether to send RequestForAggregatedMeasuredData to Wholesale or handle it in EDI.
    /// </summary>
    Task<bool> RequestStaysInEdiAsync();

    /// <summary>
    /// Whether to allow receiving metered data for measurement points.
    /// </summary>
    Task<bool> ReceiveMeteredDataForMeasurementPointsAsync();

    /// <summary>
    /// Whether to use the RequestWholesaleServices process orchestration.
    /// </summary>
    Task<bool> UseRequestWholesaleServicesProcessOrchestrationAsync();
}
