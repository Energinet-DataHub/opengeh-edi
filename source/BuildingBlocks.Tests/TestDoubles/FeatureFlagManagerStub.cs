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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;

/// <summary>
/// A FeatureFlagManager used to set default values and which allows overriding feature flags during tests
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Tests")]
public class FeatureFlagManagerStub : IFeatureFlagManager
{
    private readonly Dictionary<string, bool> _featureFlagDictionary = new()
    {
        { FeatureFlagName.UsePeekMessages, true },
        { FeatureFlagName.PM25CIM, true },
        { FeatureFlagName.PM25Ebix, true },
        { FeatureFlagName.PeekMeasurementMessages, true },
    };

    public void SetFeatureFlag(string featureFlagName, bool value)
    {
        _featureFlagDictionary[featureFlagName] = value;
    }

    public Task<bool> UsePeekMessagesAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.UsePeekMessages]);

    public Task<bool> UsePeekForwardMeteredDataMessagesAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.PeekMeasurementMessages]);

    public Task<bool> ReceiveForwardMeteredDataInCimAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.PM25CIM]);

    public Task<bool> ReceiveForwardMeteredDataInEbixAsync() => Task.FromResult(_featureFlagDictionary[FeatureFlagName.PM25Ebix]);
}
