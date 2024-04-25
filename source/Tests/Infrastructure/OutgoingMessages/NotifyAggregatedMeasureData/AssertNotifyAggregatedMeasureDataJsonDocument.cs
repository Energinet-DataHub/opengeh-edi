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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using Json.Schema;
using Xunit;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public sealed class AssertNotifyAggregatedMeasureDataJsonDocument : IAssertNotifyAggregatedMeasureDataDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertNotifyAggregatedMeasureDataJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("NotifyAggregatedMeasureData_MarketDocument");
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMessageId(string expectedMessageId)
    {
        Assert.Equal(expectedMessageId, _root.GetProperty("mRID").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument MessageIdExists()
    {
        _root.TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSenderId(string expectedSenderId)
    {
        Assert.Equal(expectedSenderId, _root.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasReceiverId(string expectedReceiverId)
    {
        Assert.Equal(expectedReceiverId, _root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTimestamp(string expectedTimestamp)
    {
        Assert.Equal(expectedTimestamp, _root.GetProperty("createdDateTime").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasTransactionId(Guid expectedTransactionId)
    {
        Assert.Equal(expectedTransactionId, FirstTimeSeriesElement().GetProperty("mRID").GetGuid());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument TransactionIdExists()
    {
        FirstTimeSeriesElement().TryGetProperty("mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        Assert.Equal(expectedGridAreaCode, FirstTimeSeriesElement()
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("value")
            .ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber)
    {
        Assert.Equal(expectedBalanceResponsibleNumber, FirstTimeSeriesElement()
            .GetProperty("balanceResponsibleParty_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber)
    {
        Assert.Equal(expectedEnergySupplierNumber, FirstTimeSeriesElement()
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasProductCode(string expectedProductCode)
    {
        Assert.Equal(expectedProductCode, FirstTimeSeriesElement().GetProperty("product").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPeriod(Period expectedPeriod)
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

    public IAssertNotifyAggregatedMeasureDataDocument HasPoint(int position, int quantity)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Equal(position, point.GetProperty("position").GetProperty("value").GetInt32());
        Assert.Equal(quantity, point.GetProperty("quantity").GetInt32());
        return this;
    }

    public async Task<IAssertNotifyAggregatedMeasureDataDocument> DocumentIsValidAsync()
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

    public IAssertNotifyAggregatedMeasureDataDocument SettlementMethodIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("marketEvaluationPoint.settlementMethod"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument EnergySupplierNumberIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("energySupplier_MarketParticipant.mRID"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument BalanceResponsibleNumberIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("balanceResponsibleParty_MarketParticipant.mRID"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QuantityIsNotPresentForPosition(int position)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Throws<KeyNotFoundException>(() => point.GetProperty("quantity"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsNotPresentForPosition(int position)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point").EnumerateArray().ToList()[position - 1];

        Assert.Throws<KeyNotFoundException>(() => point.GetProperty("quality"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument QualityIsPresentForPosition(
        int position,
        string quantityQualityCode)
    {
        var quality = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[position - 1]
            .GetProperty("quality")
            .GetProperty("value");

        quality.Should().NotBeNull();
        quality.ToString().Should().Be(quantityQualityCode);

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasCalculationResultVersion(long version)
    {
        Assert.Equal(version.ToString(NumberFormatInfo.InvariantInfo), FirstTimeSeriesElement().GetProperty("version").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasMeteringPointType(MeteringPointType meteringPointType)
    {
        FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(meteringPointType.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasQuantityMeasurementUnit(MeasurementUnit quantityMeasurementUnit)
    {
        FirstTimeSeriesElement()
            .GetProperty("quantity_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(quantityMeasurementUnit.Code);
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasResolution(Resolution resolution)
    {
        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("resolution")
            .GetString()
            .Should()
            .Be(resolution.Code);

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasPoints(IReadOnlyCollection<TimeSeriesPoint> points)
    {
        var pointsInDocument = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .OrderBy(p => p.GetProperty("position")
                .GetProperty("value")
                .GetInt32())
            .ToList();

        pointsInDocument.Should().HaveSameCount(points);

        var expectedPoints = points.OrderBy(p => p.Time).ToList();

        for (var i = 0; i < pointsInDocument.Count; i++)
        {
            pointsInDocument[i]
                .GetProperty("position")
                .GetProperty("value")
                .GetInt32()
                .Should()
                .Be(i + 1);

            pointsInDocument[i]
                .GetProperty("quantity")
                .GetDecimal()
                .Should()
                .Be(expectedPoints[i].Quantity.ToDecimal());

            var expectedQuantityQuality = expectedPoints[i].QuantityQualities.Single() switch
            {
                QuantityQuality.Calculated => CimCode.QuantityQualityCodeCalculated,
                QuantityQuality.Estimated => CimCode.QuantityQualityCodeEstimated,
                QuantityQuality.Measured => CimCode.QuantityQualityCodeMeasured,
                _ => throw new NotImplementedException(
                    $"Quantity quality {expectedPoints[i].QuantityQualities.Single()} not implemented"),
            };

            pointsInDocument[i]
                .GetProperty("quality")
                .GetProperty("value")
                .GetString()
                .Should()
                .Be(expectedQuantityQuality);
        }

        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasBusinessReason(BusinessReason businessReason)
    {
        Assert.Equal(businessReason.Code, _root.GetProperty("process.processType").GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementVersion(SettlementVersion settlementVersion)
    {
        Assert.Equal(settlementVersion.Code, FirstTimeSeriesElement().GetProperty("settlement_Series.version").GetProperty("value").ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument SettlementVersionIsNotPresent()
    {
        Assert.Throws<KeyNotFoundException>(() => FirstTimeSeriesElement().GetProperty("settlement_Series.version"));
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasOriginalTransactionIdReference(string originalTransactionIdReference)
    {
        Assert.Equal(originalTransactionIdReference, FirstTimeSeriesElement()
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .ToString());
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument OriginalTransactionIdReferenceDoesNotExist()
    {
        FirstTimeSeriesElement()
            .TryGetProperty("originalTransactionIDReference_Series.mRID", out _).Should().BeTrue();
        return this;
    }

    public IAssertNotifyAggregatedMeasureDataDocument HasSettlementMethod(SettlementMethod settlementMethod)
    {
        Assert.Equal(settlementMethod.Code, FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod").GetProperty("value")
            .ToString());
        return this;
    }

    private JsonElement FirstTimeSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }
}
