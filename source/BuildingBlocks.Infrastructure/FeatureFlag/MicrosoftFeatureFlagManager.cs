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

using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

/// <summary>
/// A <see cref="IFeatureFlagManager"/> implementation using the Microsoft.FeatureManagement package
/// </summary>
public class MicrosoftFeatureFlagManager(
    IVariantFeatureManager featureManager,
    IConfigurationRefresherProvider refresherProvider)
    : IFeatureFlagManager
{
    private readonly IVariantFeatureManager _featureManager = featureManager;
    private readonly IConfigurationRefresher _refresher = refresherProvider.Refreshers.First();

    public Task<bool> UsePeekMessagesAsync() => IsEnabledAsync(FeatureFlagName.UsePeekMessages);

    public Task<bool> UseRequestWholesaleServicesProcessOrchestrationAsync() => IsEnabledAsync(FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration);

    public Task<bool> UseRequestAggregatedMeasureDataProcessOrchestrationAsync() => IsEnabledAsync(FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration);

    public Task<bool> UseProcessManagerToEnqueueBrs023027MessagesAsync() => IsEnabledAsync(FeatureFlagName.UseProcessManagerToEnqueueBrs023027Messages);

    // Product Goals
    public Task<bool> ReceiveMeteredDataForMeasurementPointsInCimAsync() => IsEnabledAsync(FeatureFlagName.PM25CIM);

    public Task<bool> ReceiveMeteredDataForMeasurementPointsInEbixAsync() => IsEnabledAsync(FeatureFlagName.PM25Ebix);

    public Task<bool> UsePeekMeasureDataMessagesAsync() => IsEnabledAsync(FeatureFlagName.PM25Messages);

    protected async Task<bool> IsEnabledAsync(string featureFlagName)
    {
        await _refresher.TryRefreshAsync().ConfigureAwait(false);
        return await _featureManager.IsEnabledAsync(featureFlagName).ConfigureAwait(false);
    }
}
