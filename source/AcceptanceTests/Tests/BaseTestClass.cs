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
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

public class BaseTestClass
{
    protected const string ActorNumber = "5790000701414";
    protected const string ActorRole = "energysupplier";

    protected BaseTestClass(ITestOutputHelper output, TestRunner runner)
    {
        Debug.Assert(runner != null, nameof(runner) + " != null");
        Output = output;
        BaseTestRunner = runner;
        Token = TokenBuilder.BuildToken(ActorNumber, new[] { ActorRole }, runner.AzpToken);
        AggregationRequest = new AggregatedMeasureDataRequestDsl(new EdiDriver(runner.AzpToken, runner.ConnectionString));
        AzureAuthenticationDriver = new AzureAuthenticationDriver(
            runner.AzureEntraTenantId,
            runner.AzureEntraBackendAppId,
            runner.AzureEntraB2CTenantUrl,
            runner.AzureEntraBackendBffScope,
            runner.AzureEntraFrontendAppId,
            new Uri("https://app-webapi-markpart-u-001.azurewebsites.net"));
    }

    protected ITestOutputHelper Output { get; }

    protected AggregatedMeasureDataRequestDsl AggregationRequest { get; }

    protected AzureAuthenticationDriver AzureAuthenticationDriver { get; }

    protected TestRunner BaseTestRunner { get; }

    protected string Token { get; set; }
}
