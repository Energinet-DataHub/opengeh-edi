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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;

/// <summary>
/// Extensions for reading feature flags in EDI.
/// The extension methods allows us to create an abstraction from the feature flag names,
/// explaining the meaning of the feature flag in EDI. This is especially relevant for
/// "Produkt Mål's" feature flags, as they might be used in multiple subsystem, but
/// mean something different in each subsystem.
/// </summary>
public static class FeatureManagerExtensions
{
    /// <summary>
    /// Whether to disallow actors to peek messages.
    /// </summary>
    public static Task<bool> UsePeekMessagesAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.UsePeekMessages);
    }

    /// <summary>
    /// Whether to allow receiving metered data for metering points in CIM.
    /// </summary>
    public static Task<bool> ReceiveForwardMeteredDataInCimAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.PM25CIM);
    }

    /// <summary>
    /// Whether to allow receiving metered data for metering points Ebix.
    /// </summary>
    public static Task<bool> ReceiveForwardMeteredDataInEbixAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.PM25Ebix);
    }

    /// <summary>
    /// Whether to disallow actors to peek time series messages.
    /// </summary>
    public static Task<bool> UsePeekMeasurementsMessagesAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.PeekMeasurementMessages);
    }

    /// <summary>
    /// Whether to disable enqueuement of request measurements messages.
    /// </summary>
    public static Task<bool> EnqueueRequestMeasurementsResponseMessagesAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.UsePM28Enqueue);
    }

    /// <summary>
    /// Whether to disallow actors to request measurements messages in CIM.
    /// </summary>
    public static Task<bool> ReceiveRequestMeasurementsMessagesInCimAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.PM28ReceiveCIMMessages);
    }

    /// <summary>
    /// Whether to disallow actors to request measurements messages in CIM.
    /// </summary>
    public static Task<bool> SyncMeasurementsAsync(this IFeatureManager featureManager)
    {
        return featureManager.IsEnabledAsync(FeatureFlagNames.SyncMeasurements);
    }
}
