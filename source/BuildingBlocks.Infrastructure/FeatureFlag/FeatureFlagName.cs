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

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

/// <summary>
/// List of all Feature Flags that exists in the subsystem.
/// The feature flags can be locally configured through app settings,
/// or exist in Azure App Configuration.
/// If configured locally the name of a feature flag configuration
/// must be prefixed with "FeatureManagement__",
/// ie. "FeatureManagement__UseMonthlyAmountPerChargeResultProduced".
/// </summary>
public static class FeatureFlagName
{
    /// <summary>
    /// Whether to disable peek messages
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
}
