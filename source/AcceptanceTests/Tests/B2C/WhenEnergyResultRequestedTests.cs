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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2C;

[Collection("Acceptance test collection")]
public class WhenEnergyResultRequestedTests : BaseTestClass
{
    private readonly FrontendDsl _frontend;

    public WhenEnergyResultRequestedTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _frontend = new FrontendDsl(
            new EdiB2CDriver(fixture.B2CAuthorizedHttpClient, fixture.ApiManagementUri, fixture.EdiB2CUri));
    }

    [Fact]
    public async Task Actor_can_request_aggregated_measure_data()
    {
       await _frontend.RequestAggregatedMeasureData(AcceptanceTestFixture.EdiSubsystemTestCimEnergySupplierNumber, CancellationToken.None);
    }
}
