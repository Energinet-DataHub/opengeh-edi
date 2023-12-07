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
using Energinet.DataHub.EDI.AcceptanceTests;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.xml;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.B2BErrors;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await.")]
public class AuthenticationErrors : TestRunner
{
    private const string ActorNumber = "5790000701414";
    private const string ActorRole = "energysupplier";
    private readonly ITestOutputHelper _output;
    private readonly AggregatedMeasureDataRequestDsl _aggregationRequest;

    public AuthenticationErrors(ITestOutputHelper output)
    {
        _output = output;
        _aggregationRequest = _aggregationRequest = new AggregatedMeasureDataRequestDsl(new EdiDriver(AzpToken, ConnectionString));
    }

    [Fact]
    public async Task WrongTokenProducesAuthenticationErrorAsync()
    {
        string response;
        ErrorResponse? responseError;
        var payload = PayloadBuilder.BuildXmlPayload(
            SynchronousErrorTestData.MismatchingSenderIdData(),
            SynchronousErrorTestData.DefaultTestData(),
            SynchronousErrorTestData.DefaultSeriesTestData());

        response = await _aggregationRequest.AggregatedMeasureDataWithXmlPayload(ActorNumber, ActorRole, payload);

        _output.WriteLine(response);

        responseError = SynchronousError.BuildB2BErrorResponse(response);

        Assert.Equal("00002", responseError?.Code);
        Assert.Equal("Sender id does not match id of current authenticated user", responseError?.Message);
    }

    /*[Fact]
    public async void MismatchingSenderIdProducesAuthenticationError()
    {
       //Console.Write(await new RequestBuilder().SendRequestXmlWithPayload("/api/RequestAggregatedMeasureMessageReceiver", new PayloadBuilder().BuildXmlPayload("5790000701414","DDQ")));
    }

    //[Fact]
    public async void MismatchingSenderRoleTypeProducesAuthenticationError()
    {
        // Console.Write(await new RequestBuilder().SendRequestXmlWithPayload("/api/RequestAggregatedMeasureMessageReceiver", new PayloadBuilder().BuildXmlPayload("5790000701414","DDQ")));
    }*/
}
