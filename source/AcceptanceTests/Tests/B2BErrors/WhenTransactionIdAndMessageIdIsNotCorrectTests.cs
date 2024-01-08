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
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Energinet.DataHub.EDI.AcceptanceTests.Tests.Asserters;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public class WhenTransactionIdAndMessageIdIsNotCorrectTests : BaseTestClass
{
    private readonly AcceptanceTestFixture _fixture;

    public WhenTransactionIdAndMessageIdIsNotCorrectTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Message_id_is_not_unique()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.MessageIdIsNotUnique());
        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, _fixture.EnergySupplierCredential);
        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, _fixture.EnergySupplierCredential);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00101", "Message id 'B6Qhv7Dls6zdnvgna3cQqXu0PAzFqKco8GLc' is not unique", response);
    }

    [Fact]
    public async Task Transaction_id_is_not_unique()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.TransactionIdIsNotUnique());

        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, _fixture.EnergySupplierCredential);
        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, _fixture.EnergySupplierCredential);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00102", "Transaction id 'aX5fNO7st0zVIemSRek4GM1FCSRbQ28PMIZO' is not unique and will not be processed.", response);
    }

    [Fact]
    public async Task Message_id_is_empty()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData
                .EmptyMessageId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, _fixture.EnergySupplierCredential);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00201", "The id of the message cannot be empty", response);
    }
}
