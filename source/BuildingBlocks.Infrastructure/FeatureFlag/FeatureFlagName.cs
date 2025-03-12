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
/// List of all Feature Flags that exists in the system. A Feature Flag name must
/// correspond to a value found in the app configuration as "FeatureManagement__NameOfFeatureFlag"
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
    /// Whether to allow actors to peek metered data for metering points.
    /// </summary>
    public const string PM25Messages = "PM25-MESSAGES";

    /// <summary>
    /// Whether to archive BRS-021 messages.
    /// </summary>
    public const string ArchiveBrs021Messages = "ArchiveBrs021Messages";
}
