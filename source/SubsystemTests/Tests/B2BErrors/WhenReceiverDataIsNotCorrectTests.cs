﻿// Copyright 2020 Energinet DataHub A/S
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
public class WhenReceiverDataIsNotCorrectTests : BaseTestClass
{
    public WhenReceiverDataIsNotCorrectTests(ITestOutputHelper output, SubsystemTestFixture fixture)
        : base(output, fixture)
    {
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Invalid_receiver_id_produces_correct_error()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(
                SynchronousErrorTestData.InvalidReceiverId());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync(
            "00303",
            "Receiver id 5790001330553 is not a valid receiver",
            response);
    }

    [Fact]
    public async Task Invalid_receiver_role_produces_correct_error()
    {
        var payload =
            RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(
                SynchronousErrorTestData.InvalidReceiverRole());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync(
            "00304",
            "Invalid receiver role",
            response);
    }
}
