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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors;

[SuppressMessage("Usage", "CA2007", Justification = "Test methods should not call ConfigureAwait(), as it may bypass parallelization limits")]

[Collection("Acceptance test collection")]
[IntegrationTest]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Acceptance Test class")]
public sealed class WhenGettingB2CTokenTests
{
    private readonly TestRunner _runner;
    private readonly AzureAuthenticationDriver _azureAuthentication;

    public WhenGettingB2CTokenTests(TestRunner runner)
    {
        _runner = runner;

        _azureAuthentication = new AzureAuthenticationDriver(
            _runner.AzureEntraTenantId,
            _runner.AzureEntraBackendAppId,
            _runner.AzureEntraB2CTenantUrl,
            _runner.AzureEntraBackendBffScope,
            _runner.AzureEntraFrontendAppId,
            new Uri("https://app-webapi-markpart-u-001.azurewebsites.net"));
    }

    [Fact]
    public async Task Get_b2c_token()
    {
        var result = await _azureAuthentication.GetB2CTokenAsync(_runner.B2cUsername, _runner.B2cPassword);

        Assert.NotNull(result);
    }
}
