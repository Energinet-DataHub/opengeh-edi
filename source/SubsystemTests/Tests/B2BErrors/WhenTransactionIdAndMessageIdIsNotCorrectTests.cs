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
using Energinet.DataHub.EDI.SubsystemTests.TestData;
using Energinet.DataHub.EDI.SubsystemTests.Tests.B2BErrors.Asserters;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[Collection(SubsystemTestCollection.SubsystemTestCollectionName)]
public class WhenTransactionIdAndMessageIdIsNotCorrectTests : BaseTestClass
{
    public WhenTransactionIdAndMessageIdIsNotCorrectTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Message_id_is_not_unique()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.MessageIdIsNotUnique());

        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);
        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00101", "Message id 'B6Qhv7Dls6zdnvgna3cQqXu0PAzFqKco8GLc' is not unique", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Message_id_is_empty()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData
                .EmptyMessageId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00201", "The id of the message cannot be empty", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Transaction_id_is_not_unique()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.TransactionIdIsNotUnique());

        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);
        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00102", "Transaction id 'aX5fNO7st0zVIemSRek4GM1FCSRbQ28PMIZO' is not unique and will not be processed.", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Transaction_id_is_empty_produces_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.EmptyTransactionId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00202", "Transaction id cannot be empty", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Transaction_id_is_invalid_produces_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.InvalidTransactionId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00205", "Transaction id invalidId is invalid. Must contain 36 characters.", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Message_id_is_not_the_correct_length_produces_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.InvalidLengthOfMessageId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00305", "Message id " + payload.GetElementsByTagName("cim:mRID")[0]?.InnerText + " is invalid. Must contain 36 characters.", response);
    }
}
