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
    private const string ActorNumber = "5790000701414";
    private const string ActorRole = "energysupplier";

    protected BaseTestClass(ITestOutputHelper output, AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        Output = output;
        BaseTestFixture = fixture;
        Token = TokenBuilder.BuildToken(ActorNumber, new[] { ActorRole }, fixture.AzpToken);
        AggregationRequest = new AggregatedMeasureDataRequestDsl(new EdiDriver(fixture.AzpToken, fixture.ConnectionString));
        AzureAuthenticationDriver = new AzureAuthenticationDriver(
            fixture.AzureEntraTenantId,
            fixture.AzureEntraBackendAppId);
    }

    protected ITestOutputHelper Output { get; }

    protected AggregatedMeasureDataRequestDsl AggregationRequest { get; }

    protected AzureAuthenticationDriver AzureAuthenticationDriver { get; }

    protected AcceptanceTestFixture BaseTestFixture { get; }

    protected string Token { get; set; }
}
