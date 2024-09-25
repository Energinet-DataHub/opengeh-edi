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
using Energinet.DataHub.EDI.SubsystemTests.Factories;
using Energinet.DataHub.EDI.SubsystemTests.Responses.Xml;
using Energinet.DataHub.EDI.SubsystemTests.TestData;
using Energinet.DataHub.EDI.SubsystemTests.Tests.B2BErrors.Asserters;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenBusinessTypeAndProcessTypeIsNotCorrectTests : BaseTestClass
{
    public WhenBusinessTypeAndProcessTypeIsNotCorrectTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Wrong_business_sector_type_produces_schema_validation_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.SchemaValidationErrorOnWrongBusinessSectorType());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        var responseError = SynchronousError.BuildB2BErrorResponse(response);
        Assert.Equal("00302", responseError!.Code);
        Assert.Contains("schema validation error", responseError.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Invalid_cim_type_produces_type_error()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(
                SynchronousErrorTestData.TypeIsNotSupported());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync(
            "00401",
            "The type E73 is not supported",
            response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Invalid_process_type_produces_type_error()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(
                SynchronousErrorTestData.ProcessTypeIsNotSupported());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync(
            "00402",
            "The process type D09 is not support",
            response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Invalid_business_type_produces_type_error()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(
                SynchronousErrorTestData.InvalidBusinessType());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync(
            "00403",
            "The business type 27 is not supported",
            response);
    }
}
