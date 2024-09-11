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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using FluentAssertions;
using Json.Schema;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public sealed class AssertRejectRequestAggregatedMeasureDataJsonDocument : IAssertRejectRequestAggregatedMeasureDataDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertRejectRequestAggregatedMeasureDataJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("RejectRequestAggregatedMeasureData_MarketDocument");
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        Assert.Equal(expectedMessageId, _root.GetProperty("mRID").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument MessageIdExists()
    {
        _root.TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        Assert.Equal(expectedSenderId, _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        Assert.Equal(expectedReceiverId, _root.GetProperty("receiver_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTimestamp(Instant expectedTimestamp)
    {
        Assert.Equal(expectedTimestamp.ToString(), _root.GetProperty("createdDateTime").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasReasonCode(string reasonCode)
    {
        Assert.Equal(reasonCode, _root.GetProperty("reason.code")
            .GetProperty("value").ToString());
        return this;
    }

    public async Task<IAssertRejectRequestAggregatedMeasureDataDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas.GetSchemaAsync<JsonSchema>("RejectRequestAggregatedMeasureData", "0", CancellationToken.None).ConfigureAwait(false);
        var validationOptions = new EvaluationOptions()
        {
            OutputFormat = OutputFormat.List,
        };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors).Select(x => x.Errors).ToList()
            .SelectMany(e => e!.Values).ToList();
        Assert.True(validationResult.IsValid, string.Join("\n", errors));
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        ArgumentNullException.ThrowIfNull(businessReason);
        Assert.Equal(businessReason.Code, _root.GetProperty("process.processType").GetProperty("value").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasTransactionId(TransactionId expectedTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedTransactionId);
        Assert.Equal(expectedTransactionId.Value, FirstSeriesElement().GetProperty("mRID").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument TransactionIdExists()
    {
        FirstSeriesElement().TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonCode(string expectedSerieReasonCode)
    {
        Assert.Equal(expectedSerieReasonCode, FirstReasonElement().GetProperty("code").GetProperty("value").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasSerieReasonMessage(string expectedSerieReasonMessage)
    {
        Assert.Equal(expectedSerieReasonMessage, FirstReasonElement().GetProperty("text").ToString());
        return this;
    }

    public IAssertRejectRequestAggregatedMeasureDataDocument HasOriginalTransactionId(
        TransactionId expectedOriginalTransactionId)
    {
        ArgumentNullException.ThrowIfNull(expectedOriginalTransactionId);
        Assert.Equal(
            expectedOriginalTransactionId.Value,
            FirstSeriesElement().GetProperty("originalTransactionIDReference_Series.mRID").ToString());
        return this;
    }

    private JsonElement FirstSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }

    private JsonElement FirstReasonElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0]
            .GetProperty("Reason").EnumerateArray().ToList()[0];
    }
}
