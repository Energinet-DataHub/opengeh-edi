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
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.xml;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public class WhenBusinessTypeAndProcessTypeIsNotCorrectTests : BaseTestClass
{
    public WhenBusinessTypeAndProcessTypeIsNotCorrectTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
    }

    [Fact]
    public async Task Wrong_business_sector_type_produces_schema_validation_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.SchemaValidationErrorOnWrongBusinessSectorType());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, Token);

        Output.WriteLine(response);

        var responseError = SynchronousError.BuildB2BErrorResponse(response);
        Assert.Equal("00302", responseError!.Code);
        Assert.Contains("schema validation error", responseError.Message, StringComparison.InvariantCultureIgnoreCase);
    }
}
