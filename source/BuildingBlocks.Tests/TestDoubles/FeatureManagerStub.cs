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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;

/// <summary>
/// A FeatureManager used to set default values and which allows overriding feature flags during tests
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Tests")]
public class FeatureManagerStub : IFeatureManager
{
    private readonly Dictionary<string, bool> _featureFlagDictionary = new()
    {
        { FeatureFlagNames.UsePeekMessages, true },
        { FeatureFlagNames.PM25CIM, true },
        { FeatureFlagNames.PM25Ebix, true },
        { FeatureFlagNames.Brs021PeekMessages, true },
    };

    public void SetFeatureFlag(string featureFlagName, bool value)
    {
        _featureFlagDictionary[featureFlagName] = value;
    }

    public IAsyncEnumerable<string> GetFeatureNamesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsEnabledAsync(string feature)
    {
        return Task.FromResult(_featureFlagDictionary[feature]);
    }

    public Task<bool> IsEnabledAsync<TContext>(string feature, TContext context)
    {
        throw new NotImplementedException();
    }
}
