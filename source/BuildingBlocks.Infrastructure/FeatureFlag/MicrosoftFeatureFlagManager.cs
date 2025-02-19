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

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

/// <summary>
/// A <see cref="IFeatureFlagManager"/> implementation using the Microsoft.FeatureManagement package
/// </summary>
public class MicrosoftFeatureFlagManager : IFeatureFlagManager
{
    private readonly IFeatureManager _featureManager;

    public MicrosoftFeatureFlagManager(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public Task<bool> UsePeekMessagesAsync() => IsEnabledAsync(FeatureFlagName.UsePeekMessages);

    public Task<bool> UsePeekTimeSeriesMessagesAsync() => IsEnabledAsync(FeatureFlagName.UsePeekTimeSeriesMessages);

    public Task<bool> RequestStaysInEdiAsync() => IsEnabledAsync(FeatureFlagName.RequestStaysInEdi);

    public Task<bool> ReceiveMeteredDataForMeasurementPointsAsync() => IsEnabledAsync(FeatureFlagName.ReceiveMeteredDataForMeasurementPoints);

    public Task<bool> UseRequestWholesaleServicesProcessOrchestrationAsync() => IsEnabledAsync(FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration);

    public Task<bool> UseRequestAggregatedMeasureDataProcessOrchestrationAsync() => IsEnabledAsync(FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration);

    public Task<bool> UseProcessManagerToEnqueueBrs023027MessagesAsync() => IsEnabledAsync(FeatureFlagName.UseProcessManagerToEnqueueBrs023027Messages);

    private Task<bool> IsEnabledAsync(FeatureFlagName featureFlagName) => _featureManager.IsEnabledAsync(featureFlagName.ToString());
}
