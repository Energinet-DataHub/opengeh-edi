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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.FeatureFlag;

/// <summary>
/// Responsible for testing the behavior of feature flags implemented in MicrosoftFeatureFlagManager.
/// MicrosoftFeatureFlagManager combines feature flags from Azure App Configuration with the Microsoft.FeatureManagement package.
/// Since MicrosoftFeatureFlagManager.IsEnabledAsync is a protected method, we need to create a spy class to test it.
/// </summary>
public class GivenFeatureFlagTests : TestBase, IAsyncDisposable
{
    private readonly string _disabledFeatureFlag = "edi-integrationtests/disabled-feature-flag";

    private readonly MicrosoftFeatureFlagManagerSpy _sut;
    private readonly IConfigurationRefresherProvider _refresherProvider;

    public GivenFeatureFlagTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        var featureManager = GetService<IFeatureManager>();
        _refresherProvider = GetService<IConfigurationRefresherProvider>();
        _sut = new MicrosoftFeatureFlagManagerSpy(featureManager, _refresherProvider);
    }

    public async ValueTask DisposeAsync()
    {
        await AppConfigurationClient.SetFeatureFlagAsync(_disabledFeatureFlag, false);
    }

    [Fact]
    public async Task Given_DisabledFeatureFlag_When_IsToggled_Then_FeatureIsEnabled()
    {
        // Arrange
        await AppConfigurationClient.SetFeatureFlagAsync(_disabledFeatureFlag, false);
        // The feature flag is updated every second.
        await Task.Delay(1020);

        // Act
        var resultBefore = await _sut.IsEnabledSpyAsync(_disabledFeatureFlag);

        await AppConfigurationClient.SetFeatureFlagAsync(_disabledFeatureFlag, true);

        // Wait for the feature flag to be updated
        // The feature flag is updated every second.
        await Task.Delay(1020);
        var isEnabled = await _sut.IsEnabledSpyAsync(_disabledFeatureFlag);

        // Assert
        Assert.False(resultBefore);
        Assert.True(isEnabled);
    }

    [Fact]
    public async Task Given_FeatureFlag_When_IsMissing_Then_FeatureIsDisabled()
    {
        // Arrange
        var missingFeatureFlag = "missing-feature-flag";

        // Act & Assert
        var isFeatureEnabled = await _sut.IsEnabledSpyAsync(missingFeatureFlag);

        // Assert
        Assert.False(isFeatureEnabled);
    }
}

/// <summary>
/// In order to test the behavior of MicrosoftFeatureFlagManager.IsEnabledAsync,
/// we need to create a spy class that inherits from MicrosoftFeatureFlagManager and changes the visibility of the method.
/// </summary>
public class MicrosoftFeatureFlagManagerSpy(
    IFeatureManager featureManager,
    IConfigurationRefresherProvider refresherProvider) : MicrosoftFeatureFlagManager(featureManager, refresherProvider)
{
    public Task<bool> IsEnabledSpyAsync(string featureFlagName)
    {
        return IsEnabledAsync(featureFlagName);
    }
}
