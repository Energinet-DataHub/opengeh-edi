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
using Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors.Asserters;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[Collection(AcceptanceTestCollection.AcceptanceTestCollectionName)]
public class WhenPayloadDataIsDifferentFromTokenDataTests : BaseTestClass
{
    public WhenPayloadDataIsDifferentFromTokenDataTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Sender_market_participant_mrid_is_different_from_mrid_in_token()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.WrongSenderMarketParticipantMrid());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00002", "Sender id does not match id of current authenticated user", response);
    }

    [Fact(Skip = "Not a sub system test")]
    public async Task Mismatching_sender_role_type_produces_authentication_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.SenderRoleTypeNotAuthorized());

        var response = await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload);

        Output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00003", "Sender role type is not authorized to use this type of message", response);
    }
}
