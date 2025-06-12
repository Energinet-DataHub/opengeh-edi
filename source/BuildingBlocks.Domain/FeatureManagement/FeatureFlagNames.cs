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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;

/// <summary>
/// Names of Feature Flags that exists in EDI.
///
/// The "Feature Flags in EDI" documentation page in confluence should be kept
/// up-to-date: https://energinet.atlassian.net/wiki/spaces/D3/pages/677412898/Feature+Flags+in+EDI
///
/// The feature flags can be configured:
///  * Using App Settings (locally or in Azure)
///  * In Azure App Configuration
///
/// If configured using App Settings, the name of a feature flag
/// configuration must be prefixed with <see cref="SectionName"/>,
/// ie. "FeatureManagement__UsePeekMessages".
/// </summary>
/// <remarks>
/// We use "const" for feature flags instead of a enum, because "Produkt Måls"
/// feature flags contain "-" in their name.
/// </remarks>
public static class FeatureFlagNames
{
    /// <summary>
    /// Configuration section name when configuring feature flags as App Settings.
    /// </summary>
    public const string SectionName = "FeatureManagement";

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
    public const string PeekMeasurementMessages = "PEEK-MEASUREMENT-MESSAGES";

    /// <summary>
    /// Whether to allow enqueuing messages for PM28 (Enqueue).
    /// </summary>
    public const string UsePM28Enqueue = "UsePM28Enqueue";

    /// <summary>
    /// Whether to allow receiving CIM request messages for PM28.
    /// </summary>
    public const string PM28ReceiveCIMMessages = "PM28-CIM";

    /// <summary>
    /// Whether to allow synchronizing measurements from DataHub 2 to EDI.
    /// </summary>
    public const string SyncMeasurements = "SyncMeasurements";
}
