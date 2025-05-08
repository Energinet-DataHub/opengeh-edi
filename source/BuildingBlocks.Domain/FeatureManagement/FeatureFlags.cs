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

using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureManagement;

/// <summary>
/// Manage feature flags in EDI.
/// The "Feature Flags in EDI" documentation page in confluence should be kept
/// up-to-date: https://energinet.atlassian.net/wiki/spaces/D3/pages/677412898/Feature+Flags+in+EDI
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Whether to disallow actors to peek messages.
    /// </summary>
    public static Task<bool> UsePeekMessagesAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(Names.UsePeekMessages);
    }

    /// <summary>
    /// Whether to allow receiving metered data for metering points in CIM.
    /// </summary>
    public static Task<bool> ReceiveForwardMeteredDataInCimAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(Names.PM25CIM);
    }

    /// <summary>
    /// Whether to allow receiving metered data for metering points Ebix.
    /// </summary>
    public static Task<bool> ReceiveForwardMeteredDataInEbixAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(Names.PM25Ebix);
    }

    /// <summary>
    /// Whether to disallow actors to peek time series messages.
    /// </summary>
    public static Task<bool> UsePeekForwardMeteredDataMessagesAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(Names.Brs021PeekMessages);
    }

    /// <summary>
    /// Names of all Feature Flags that exists in EDI.
    ///
    /// The feature flags can be configured:
    ///  * Locally through app settings
    ///  * In Azure App Configuration
    ///
    /// If configured locally the name of a feature flag configuration
    /// must be prefixed with "FeatureManagement__",
    /// ie. "FeatureManagement__UsePeekMessages".
    /// </summary>
    /// <remarks>
    /// We use "const" for feature flags instead of a enum, because "Produkt Måls"
    /// feature flags contain "-" in their name.
    /// </remarks>
    public static class Names
    {
        /// <summary>
        /// Whether to disable peek messages.
        /// </summary>
        public const string UsePeekMessages = "UsePeekMessages";

        /// <summary>
        /// Whether to allow receiving metered data for metering points in CIM.
        /// </summary>
        public const string PM25CIM = "PM25-CIM";

        /// <summary>
        /// Whether to allow receiving metered data for metering points in Ebix.
        /// </summary>
        public const string PM25Ebix = "PM25-EBIX";

        /// <summary>
        /// Whether to allow actors to peek measurements messages.
        /// </summary>
        public const string Brs021PeekMessages = "BRS021-PEEK-MESSAGES";
    }
}
