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
using Energinet.DataHub.EDI.AcceptanceTests.Dsl;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests.ArchivedMessages;

[Collection("Acceptance test collection")]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Testing")]
public class WhenArchivedMessageIsRequestedTests : BaseTestClass
{
    private readonly ArchivedMessageDsl _archivedMessage;
    private readonly AcceptanceTestFixture _fixture;
    private readonly AggregationResultDsl _aggregationResult;

    public WhenArchivedMessageIsRequestedTests(ITestOutputHelper output, AcceptanceTestFixture fixture)
        : base(output, fixture)
    {
        Debug.Assert(fixture != null, nameof(fixture) + " != null");
        _fixture = fixture;
        _archivedMessage = new ArchivedMessageDsl(
            new AzureAuthenticationDriver(
                fixture.AzureEntraTenantId,
                fixture.AzureEntraBackendAppId),
            new EdiB2CDriver(fixture.B2CAuthorizedHttpClient));
        _aggregationResult = new AggregationResultDsl(
            new EdiDriver(fixture.MeteredDataResponsibleAzpToken, fixture.ConnectionString, fixture.EdiB2BBaseUri),
            new WholesaleDriver(fixture.EventPublisher));
    }

    [Fact(Skip = "Currently environments are not setup")]
    public async Task Archived_message_is_created_after_aggregated_measure_data_request()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload();
        var messageId = payload?.GetElementsByTagName("cim:mRID")[0]?.InnerText;

        if (payload != null) await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, Token);

        var response = await _archivedMessage.RequestArchivedMessageSearchAsync(
            new Uri(_fixture.B2CApiUri, "ArchivedMessageSearch"),
            ArchivedMessageData.GetSearchableDataObject(
                messageId!,
                null!,
                null!,
                null!,
                null!));

        await _aggregationResult.ConfirmResultIsAvailableForToken(Token);

        Assert.NotNull(response[0].Id);
    }

    [Fact(Skip = "Currently environments are not setup")]
    public async Task Archived_message_is_getable_after_peek()
     {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload();

        var messageId = payload?.GetElementsByTagName("cim:mRID")[0]?.InnerText;

        if (payload != null) await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, Token);

        await _aggregationResult.ConfirmResultIsAvailableForToken(Token);

        var archivedRequestResponse = await _archivedMessage.RequestArchivedMessageSearchAsync(
            new Uri(_fixture.B2CApiUri, "ArchivedMessageSearch"),
            ArchivedMessageData.GetSearchableDataObject(
                messageId!,
                null!,
                null!,
                null!,
                null!));

        var response = await _archivedMessage.ArchivedMessageGetDocumentAsync(new Uri(_fixture.B2CApiUri, "/ArchivedMessageGetDocument?id=" + archivedRequestResponse[0].Id));

        Assert.Equal(payload?.OuterXml, response);
     }

    [Fact(Skip = "Currently environments are not setup")]
    public async Task Archived_messages_is_returned_with_correct_format()
    {
        var payload = RequestAggregatedMeasureXmlBuilder.BuildEnergySupplierXmlPayload();

        await AggregationRequest.AggregatedMeasureDataWithXmlPayload(payload, Token);

        var messageId = payload?.GetElementsByTagName("cim:mRID")[0]?.InnerText;

        var response = await _archivedMessage.RequestArchivedMessageSearchAsync(
            new Uri(_fixture.B2CApiUri, "ArchivedMessageSearch"),
            ArchivedMessageData.GetSearchableDataObject(
                messageId!,
                null!,
                null!,
                null!,
                null!));

        var archivedMessage = response[0];

        await _aggregationResult.ConfirmResultIsAvailableForToken(Token);

        Assert.NotNull(archivedMessage.Id);
        Assert.NotNull(archivedMessage.MessageId);
        Assert.NotNull(archivedMessage.DocumentType);
        Assert.NotNull(archivedMessage.SenderNumber);
        Assert.NotNull(archivedMessage.ReceiverNumber);
        Assert.IsType<DateTime>(archivedMessage.CreatedAt);
        Assert.NotNull(archivedMessage.BusinessReason);
    }
}
