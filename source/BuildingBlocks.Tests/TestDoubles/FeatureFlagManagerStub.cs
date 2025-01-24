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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;

/// <summary>
/// A FeatureFlagManager used to set default values and which allows overriding feature flags during tests
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Tests")]
public class FeatureFlagManagerStub : IFeatureFlagManager
{
    private readonly Dictionary<FeatureFlagName, bool> _featureFlagDictionary = new()
    {
        { FeatureFlagName.UsePeekMessages, true },
        { FeatureFlagName.UsePeekTimeSeriesMessages, true },
        { FeatureFlagName.RequestStaysInEdi, false },
        { FeatureFlagName.ReceiveMeteredDataForMeasurementPoints, true },
        { FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration, false },
        { FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration, false },
        { FeatureFlagName.EnqueueBrs023027MessagesViaProcessManager, false },
    };

    public void SetFeatureFlag(FeatureFlagName featureFlagName, bool value)
    {
        _featureFlagDictionary[featureFlagName] = value;
    }

    public Task<bool> UsePeekMessagesAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.UsePeekMessages]);

    public Task<bool> UsePeekTimeSeriesMessagesAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.UsePeekTimeSeriesMessages]);

    public Task<bool> RequestStaysInEdiAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.RequestStaysInEdi]);

    public Task<bool> ReceiveMeteredDataForMeasurementPointsAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.ReceiveMeteredDataForMeasurementPoints]);

    public Task<bool> UseRequestWholesaleServicesProcessOrchestrationAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration]);

    public Task<bool> UseRequestAggregatedMeasureDataProcessOrchestrationAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration]);

    public Task<bool> EnqueueBrs023027MessagesViaProcessManagerAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.EnqueueBrs023027MessagesViaProcessManager]);
}
