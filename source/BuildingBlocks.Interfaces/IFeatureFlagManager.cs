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

namespace Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

/// <summary>
/// Manage feature flags in the application.
/// The feature flags are managed through the app configuration, and the name
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
    /// Whether to disallow actors to peek time series messages.
    /// </summary>
    Task<bool> UsePeekForwardMeteredDataMessagesAsync();

    /// <summary>
    /// Whether to allow receiving metered data for metering points in CIM.
    /// </summary>
    Task<bool> ReceiveForwardMeteredDataInCimAsync();

    /// <summary>
    /// Whether to allow receiving metered data for metering points Ebix.
    /// </summary>
    Task<bool> ReceiveForwardMeteredDataInEbixAsync();

    /// <summary>
    /// Whether to archive BRS-021 messages.
    /// </summary>
    Task<bool> ArchiveBrs021MessagesAsync();
}
