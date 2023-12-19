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

using System.Diagnostics;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.EventHandlers;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.ArchivedMessages;

[Collection("Acceptance test collection")]
public class WhenArchivedMessageIsRequested : BaseTestClass
{
    private readonly AzureAuthenticationDriver _azureAuthenticationDriver;
    private readonly Task<string> _azureToken;

    public WhenArchivedMessageIsRequested(ITestOutputHelper output, TestRunner runner)
        : base(output, runner)
    {
        Debug.Assert(runner != null, nameof(runner) + " != null");
        _azureAuthenticationDriver = new AzureAuthenticationDriver(runner.AzureEntraTenantId, runner.AzureEntraBackendAppId);
        _azureToken =
            _azureAuthenticationDriver.GetAzureAdTokenAsync(runner.AzureEntraClientId, runner.AzureEntraClientSecret);
    }


    [Fact]
    public Task Test_name()
    {
        Output.WriteLine(_azureToken);
    }

    [Fact]
    public async Task Test_name()
    {
    }
}
