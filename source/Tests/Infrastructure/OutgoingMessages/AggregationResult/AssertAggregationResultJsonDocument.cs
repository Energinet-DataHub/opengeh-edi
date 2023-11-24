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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Json.Schema;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.AggregationResult;

internal sealed class AssertAggregationResultJsonDocument : IAssertAggregationResultDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertAggregationResultJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("NotifyAggregatedMeasureData_MarketDocument");
    }

    public IAssertAggregationResultDocument HasMessageId(string expectedMessageId)
    {
        Assert.Equal(expectedMessageId, _root.GetProperty("mRID").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasSenderId(string expectedSenderId)
    {
        Assert.Equal(expectedSenderId, _root.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId)
    {
        Assert.Equal(expectedReceiverId, _root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp)
    {
        Assert.Equal(expectedTimestamp, _root.GetProperty("createdDateTime").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        Assert.Equal(expectedTransactionId, FirstTimeSeriesElement().GetProperty("mRID").GetGuid());
        return this;
    }

    public IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        Assert.Equal(expectedGridAreaCode, FirstTimeSeriesElement()
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("value")
            .ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        Assert.Equal(expectedBalanceResponsibleNumber, FirstTimeSeriesElement()
            .GetProperty("balanceResponsibleParty_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        Assert.Equal(expectedEnergySupplierNumber, FirstTimeSeriesElement()
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasProductCode(string expectedProductCode)
    {
        Assert.Equal(expectedProductCode, FirstTimeSeriesElement().GetProperty("product").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasPeriod(Period expectedPeriod)
    {
        Assert.Equal(expectedPeriod.StartToString(), FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("start")
            .GetProperty("value").ToString());
        Assert.Equal(expectedPeriod.EndToString(), FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("end")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasPoint(int position, int quantity)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Equal(position, point.GetProperty("position").GetProperty("value").GetInt32());
        Assert.Equal(quantity, point.GetProperty("quantity").GetInt32());
        return this;
    }

    public async Task<IAssertAggregationResultDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas.GetSchemaAsync<JsonSchema>("NOTIFYAGGREGATEDMEASUREDATA", "0", CancellationToken.None).ConfigureAwait(false);
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

    public IAssertAggregationResultDocument SettlementMethodIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("marketEvaluationPoint.settlementMethod"));
        return this;
    }

    public IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("energySupplier_MarketParticipant.mRID"));
        return this;
    }

    public IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("balanceResponsibleParty_MarketParticipant.mRID"));
        return this;
    }

    public IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Throws<KeyNotFoundException>(() => point.GetProperty("quantity"));
        return this;
    }

    public IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Throws<KeyNotFoundException>(() => point.GetProperty("quality"));
        return this;
    }

    public IAssertAggregationResultDocument HasBusinessReason(BusinessReason businessReason)
    {
        Assert.Equal(CimCode.Of(businessReason), _root.GetProperty("process.processType").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        Assert.Equal(CimCode.Of(settlementVersion), FirstTimeSeriesElement().GetProperty("settlement_Series.version").GetProperty("value").ToString());
        return this;
    }

    public IAssertAggregationResultDocument SettlementVersionIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("settlement_Series.version"));
        return this;
    }

    public IAssertAggregationResultDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        Assert.Equal(originalTransactionIdReference, FirstTimeSeriesElement()
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .ToString());
        return this;
    }

    public IAssertAggregationResultDocument HasSettlementMethod(SettlementType settlementMethod)
    {
        Assert.Equal(CimCode.Of(settlementMethod), FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod").GetProperty("value")
            .ToString());
        return this;
    }

    private JsonElement FirstTimeSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }
}
