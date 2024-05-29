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

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

public class BaseTestClass
{
    private readonly AcceptanceTestFixture _fixture;
    private readonly Lazy<AggregatedMeasureDataRequestDsl> _aggregationRequest;

    protected BaseTestClass(ITestOutputHelper output, AcceptanceTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
        _fixture.Logger = output;
        Output = output;
        _aggregationRequest = new Lazy<AggregatedMeasureDataRequestDsl>(CreateAggregatedMeasureDataRequestDsl);
    }

    protected ITestOutputHelper Output { get; }

    protected AggregatedMeasureDataRequestDsl AggregationRequest => _aggregationRequest.Value;

    private AggregatedMeasureDataRequestDsl CreateAggregatedMeasureDataRequestDsl()
    {
        return new AggregatedMeasureDataRequestDsl(
            new EdiDriver(
                _fixture.B2BEnergySupplierAuthorizedHttpClient,
                Output),
            new EdiProcessesDriver(_fixture.ConnectionString),
            new WholesaleDriver(_fixture.EventPublisher, _fixture.EdiInboxClient));
    }
}
