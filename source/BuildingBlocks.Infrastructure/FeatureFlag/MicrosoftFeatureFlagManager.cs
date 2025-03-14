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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

/// <summary>
/// A <see cref="IFeatureFlagManager"/> implementation that combines feature flags from Azure App Configuration
/// with the Microsoft.FeatureManagement package.
/// </summary>
public class MicrosoftFeatureFlagManager(
    IFeatureManager featureManager,
    IConfigurationRefresherProvider refresherProvider)
    : IFeatureFlagManager
{
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IConfigurationRefresher _refresher = refresherProvider.Refreshers.First();

    public Task<bool> UsePeekMessagesAsync() => IsEnabledAsync(FeatureFlagName.UsePeekMessages);

    public Task<bool> ArchiveBrs021MessagesAsync() => IsEnabledAsync(FeatureFlagName.ArchiveBrs021Messages);

    // Product Goals
    public Task<bool> ReceiveForwardMeteredDataInCimAsync() => IsEnabledAsync(FeatureFlagName.PM25CIM);

    public Task<bool> ReceiveForwardMeteredDataInEbixAsync() => IsEnabledAsync(FeatureFlagName.PM25Ebix);

    public Task<bool> UsePeekForwardMeteredDataMessagesAsync() => IsEnabledAsync(FeatureFlagName.PM25Messages);

    protected async Task<bool> IsEnabledAsync(string featureFlagName)
    {
        await _refresher.TryRefreshAsync().ConfigureAwait(false);
        return await _featureManager.IsEnabledAsync(featureFlagName).ConfigureAwait(false);
    }
}
