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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers.Ebix;
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.Responses.json;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.ArchivedMessages;

[Collection("Acceptance test collection")]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Testing")]
public class WhenArchivedMessageIsRequestedTests : BaseTestClass
{
    private readonly AcceptanceTestFixture _fixture;
    private readonly ArchivedMessageDsl _archivedMessage;

    public WhenArchivedMessageIsRequestedTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        Debug.Assert(fixture != null, nameof(fixture) + " != null");
        _fixture = fixture;
        _archivedMessage = new ArchivedMessageDsl(
            new AzureAuthenticationDriver(
                fixture.AzureEntraTenantId,
                fixture.AzureEntraBackendAppId),
            new B2CDriver(fixture.B2CAuthorizedHttpClient));
    }

    [Fact]
    public async Task Archived_message_is_searchable_after_peek()
    {
        var b2CToken = await _archivedMessage.GetTokenForActorAsync(_testRunner.B2cUsername, _testRunner.B2cPassword);
        var response = await _archivedMessage.RequestArchivedMessageSearchAsync(
            b2CToken,
            ArchivedMessageData.GetSearchableDataObject(
                "3da757e4-2a9c-486d-a39a-d48addf8b965",
                null!,
                null!,
                null!,
                null!));

        var id = response![0].Id;
        foreach (var item in response!)
        {
            Output.WriteLine($"Id: {item.Id}");
            Output.WriteLine($"MessageId: {item.MessageId}");
            Output.WriteLine($"DocumentType: {item.DocumentType}");
            Output.WriteLine($"SenderNumber: {item.SenderNumber}");
            Output.WriteLine($"ReceiverNumber: {item.ReceiverNumber}");
            Output.WriteLine($"CreatedAt: {item.CreatedAt}");
            Output.WriteLine($"BusinessReason: {item.BusinessReason}");
        }
    }

    [Fact]
    public async Task Archived_message_is_getable_after_peek()
    {
        _azureToken = await await _archivedMessage.GetTokenForActorAsync(_testRunner.AzureEntraClientId, _testRunner.AzureEntraClientSecret);

        Assert.NotSame(Token, _azureToken);
    }

    [Fact]
    public async Task Archived_message_is_created_after_aggregated_measure_data_request()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload();
        var messageId = payload.SelectNodes("/cim:RequestAggregatedMeasureData_MarketDocument/cim:mRID");

        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, Token);

        var b2CToken = await _archivedMessage.GetTokenForActorAsync(_testRunner.AzureEntraClientId, _testRunner.AzureEntraClientSecret);

        var response = await _archivedMessage.RequestArchivedMessageSearchAsync(
            b2CToken,
            ArchivedMessageData.GetSearchableDataObject(
                "3da757e4-2a9c-486d-a39a-d48addf8b965",
                null!,
                null!,
                null!,
                null!));
    }

    [Fact]
    public async Task Archived_messages_is_returned_with_correct_format()
    {
        var b2CToken = await _archivedMessage.GetTokenForActorAsync(_testRunner.B2cUsername, _testRunner.B2cPassword);
        var response = await _archivedMessage.RequestArchivedMessageSearchAsync(
            b2CToken,
            ArchivedMessageData.GetSearchableDataObject(
                "3da757e4-2a9c-486d-a39a-d48addf8b965",
                null!,
                null!,
                null!,
                null!));

        var archivedMessage = response[0];
        Assert.NotNull(archivedMessage.Id);
        Assert.NotNull(archivedMessage.MessageId);
        Assert.NotNull(archivedMessage.DocumentType);
        Assert.NotNull(archivedMessage.SenderNumber);
        Assert.NotNull(archivedMessage.ReceiverNumber);
        Assert.IsType<DateTime>(archivedMessage.CreatedAt);
        Assert.NotNull(archivedMessage.BusinessReason);
    }
}
