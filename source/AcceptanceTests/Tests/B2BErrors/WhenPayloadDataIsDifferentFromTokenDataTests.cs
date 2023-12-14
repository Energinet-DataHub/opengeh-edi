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
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Energinet.DataHub.EDI.AcceptanceTests.Tests.Asserters;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Testing")]
[Collection(TestRunner.AcceptanceTestCollection)]
public class WhenPayloadDataIsDifferentFromTokenDataTests
{
    private const string ActorNumber = "5790000701414";
    private const string ActorRole = "energysupplier";
    private readonly ITestOutputHelper _output;
    private readonly AggregatedMeasureDataRequestDsl _aggregationRequest;

    public WhenPayloadDataIsDifferentFromTokenDataTests(ITestOutputHelper output, TestRunner runner)
    {
        _output = output;
        _aggregationRequest = _aggregationRequest = new AggregatedMeasureDataRequestDsl(new EdiDriver(runner.AzpToken, runner.ConnectionString));
    }

    [Fact]
    public async Task Sender_market_participant_mrid_is_different_from_mrid_in_token()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.WrongSenderMarketParticipantMrid());

        var response = await _aggregationRequest.AggregatedMeasureDataWithXmlPayload(ActorNumber, ActorRole, payload);

        _output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00002", "Sender id does not match id of current authenticated user", response);
    }

    [Fact]
    public async Task Mismatching_sender_role_type_produces_authentication_error()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload(SynchronousErrorTestData.SenderRoleTypeNotAuthorized());

        var response = await _aggregationRequest.AggregatedMeasureDataWithXmlPayload(ActorNumber, ActorRole, payload);

        _output.WriteLine(response);

        await ErrorAsserter.AssertCorrectErrorIsReturnedAsync("00003", "Sender role type is not authorized to use this type of message", response);
    }
}
