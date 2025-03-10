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

using Azure.Data.AppConfiguration;
using Azure.Identity;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.FeatureFlag;

public class GivenFeatureFlagTests : TestBase
{
    public GivenFeatureFlagTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task Given_DisabledFeatureFlag_When_IsToggled_Then_FeatureIsEnabled()
    {
        // Arrange
        var featureManager = GetService<IVariantFeatureManager>();
        var refresherProvider = GetService<IConfigurationRefresherProvider>();
        var featureFlagManager = new MicrosoftFeatureFlagManagerSpy(featureManager, refresherProvider);
        var featureFlagToggler = new FeatureFlagToggler(Fixture.IntegrationTestConfiguration.Configuration["AZURE-APP-CONFIGURATION-ENDPOINT"]!);

        var disabledFeatureFlag = "edi-integrationtests/disabled-feature-flag";
        await featureFlagToggler.SetFeatureFlagAsync(disabledFeatureFlag, false);
        await refresherProvider.Refreshers.First().TryRefreshAsync();

        // Act
        var resultBefore = await featureFlagManager.IsEnabledSpyAsync(disabledFeatureFlag);

        await featureFlagToggler.SetFeatureFlagAsync(disabledFeatureFlag, true);

        // Wait for the feature flag to be updated
        // The feature flag is updated every second.
        await Task.Delay(1020);
        var isEnabled = await featureFlagManager.IsEnabledSpyAsync(disabledFeatureFlag);

        // Assert
        Assert.False(resultBefore);
        Assert.True(isEnabled);

        // Clean up
        await featureFlagToggler.SetFeatureFlagAsync(disabledFeatureFlag, false);
    }

    [Fact]
    public async Task Given_FeatureFlag_When_IsMissing_Then_FeatureIsDisabled()
    {
        // Arrange
        var featureManager = GetService<IVariantFeatureManager>();
        var refresherProvider = GetService<IConfigurationRefresherProvider>();

        var featureFlagManager = new MicrosoftFeatureFlagManagerSpy(featureManager, refresherProvider);

        var missingFeatureFlag = "missing-feature-flag";

        // Act & Assert
        var isFeatureEnabled = await featureFlagManager.IsEnabledSpyAsync(missingFeatureFlag);

        // Assert
        Assert.False(isFeatureEnabled);
    }
}

public class MicrosoftFeatureFlagManagerSpy(
    IVariantFeatureManager featureManager,
    IConfigurationRefresherProvider refresherProvider) : MicrosoftFeatureFlagManager(featureManager, refresherProvider)
{
    public Task<bool> IsEnabledSpyAsync(string featureFlagName)
    {
        return IsEnabledAsync(featureFlagName);
    }
}

public class FeatureFlagToggler
{
    private readonly ConfigurationClient _client;

    public FeatureFlagToggler(string appConfigEndpoint)
    {
        _client = new ConfigurationClient(new Uri(appConfigEndpoint), new DefaultAzureCredential());
    }

    public async Task SetFeatureFlagAsync(string featureFlagName, bool isEnabled)
    {
        var featureFlagKey = $"edi-integrationtests/{featureFlagName}";
        // var response = await _client.GetConfigurationSettingAsync(featureFlagKey);
        // var featureFlag = response.Value;
        //
        // featureFlag.Value = featureFlag.Value.Replace(
        //     "\"enabled\":false", $"\"enabled\":{isEnabled.ToString().ToLower()}");

        await _client.SetConfigurationSettingAsync(new FeatureFlagConfigurationSetting(featureFlagName, isEnabled));
    }
}
