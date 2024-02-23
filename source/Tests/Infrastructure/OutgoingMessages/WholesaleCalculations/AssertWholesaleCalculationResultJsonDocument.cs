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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using FluentAssertions;
using IncomingMessages.Infrastructure.DocumentValidation;
using Json.Schema;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.WholesaleCalculations;

public sealed class AssertWholesaleCalculationResultJsonDocument : IAssertWholesaleCalculationResultDocument
{
    private readonly JsonSchemaProvider _schemas = new(new CimJsonSchemas());
    private readonly JsonDocument _document;
    private readonly JsonElement _root;

    public AssertWholesaleCalculationResultJsonDocument(Stream document)
    {
        _document = JsonDocument.Parse(document);
        _root = _document.RootElement.GetProperty("NotifyWholesaleServices_MarketDocument");
    }

    public async Task<IAssertWholesaleCalculationResultDocument> DocumentIsValidAsync()
    {
        var schema = await _schemas
            .GetSchemaAsync<JsonSchema>("NOTIFYWHOLESALESERVICES", "0", CancellationToken.None)
            .ConfigureAwait(false);

        schema.Should().NotBeNull("Cannot validate document without a schema");

        var validationOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        var validationResult = schema!.Evaluate(_document, validationOptions);
        var errors = validationResult.Details.Where(detail => detail.HasErrors)
            .Select(x => (x.InstanceLocation, x.EvaluationPath, x.Errors))
            .SelectMany(
                p => p.Errors!.Values.Select(
                    e => $"==> '{p.InstanceLocation}' does not adhere to '{p.EvaluationPath}' with error: {e}\n"));

        validationResult.IsValid.Should().BeTrue(string.Join("\n", errors));

        return this;
    }

    #region header validation

    public IAssertWholesaleCalculationResultDocument HasMessageId(string expectedMessageId)
    {
        _root.GetProperty("mRID").GetString().Should().Be(expectedMessageId);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasBusinessReason(
        BusinessReason expectedBusinessReason,
        CodeListType codeListType)
    {
        _root.GetProperty("process.processType")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(CimCode.Of(expectedBusinessReason));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderId(ActorNumber expectedSenderId)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderId);
        _root.GetProperty("sender_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSenderId.Value);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSenderRole(ActorRole expectedSenderRole)
    {
        ArgumentNullException.ThrowIfNull(expectedSenderRole);
        _root.GetProperty("sender_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSenderRole.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverId(ActorNumber expectedReceiverId)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverId);
        _root.GetProperty("receiver_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedReceiverId.Value);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasReceiverRole(ActorRole expectedReceiverRole, CodeListType codeListType)
    {
        ArgumentNullException.ThrowIfNull(expectedReceiverRole);
        _root.GetProperty("receiver_MarketParticipant.marketRole.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedReceiverRole.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasTimestamp(string expectedTimestamp)
    {
        _root.GetProperty("createdDateTime").GetString().Should().Be(expectedTimestamp);
        return this;
    }

    #endregion

    public IAssertWholesaleCalculationResultDocument HasTransactionId(Guid expectedTransactionId)
    {
        FirstTimeSeriesElement().GetProperty("mRID").GetString().Should().Be(expectedTransactionId.ToString());
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCalculationVersion(int expectedVersion)
    {
        FirstTimeSeriesElement()
            .GetProperty("version")
            .GetString()
            .Should()
            .Be(expectedVersion.ToString(CultureInfo.InvariantCulture));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementVersion(SettlementVersion expectedSettlementVersion)
    {
        FirstTimeSeriesElement()
            .GetProperty("settlement_Series.version")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(CimCode.Of(expectedSettlementVersion));

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasOriginalTransactionIdReference(
        string expectedOriginalTransactionIdReference)
    {
        FirstTimeSeriesElement()
            .GetProperty("originalTransactionIDReference_Series.mRID")
            .GetString()
            .Should()
            .Be(expectedOriginalTransactionIdReference);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasSettlementMethod(SettlementType expectedSettlementMethod)
    {
        ArgumentNullException.ThrowIfNull(expectedSettlementMethod);

        FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedSettlementMethod.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument PriceAmountIsPresentForPointIndex(int pointIndex, string? expectedPrice)
    {
        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[pointIndex]
            .GetProperty("price.amount")
            .GetProperty("value")
            .GetDecimal()
            .ToString(NumberFormatInfo.InvariantInfo)
            .Should()
            .Be(expectedPrice);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasMeteringPointType(MeteringPointType expectedMeteringPointType)
    {
        ArgumentNullException.ThrowIfNull(expectedMeteringPointType);
        FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeteringPointType.Code);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeCode(string expectedChargeTypeNumber)
    {
        FirstTimeSeriesElement()
            .GetProperty("chargeType.mRID")
            .GetString()
            .Should()
            .Be(expectedChargeTypeNumber);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeType(ChargeType expectedChargeType)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeType);
        FirstTimeSeriesElement()
            .GetProperty("chargeType.type")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedChargeType.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasChargeTypeOwner(ActorNumber expectedChargeTypeOwner)
    {
        ArgumentNullException.ThrowIfNull(expectedChargeTypeOwner);
        FirstTimeSeriesElement()
            .GetProperty("chargeType.chargeTypeOwner_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedChargeTypeOwner.Value);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasGridAreaCode(string expectedGridAreaCode)
    {
        FirstTimeSeriesElement()
            .GetProperty("meteringGridArea_Domain.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedGridAreaCode);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasEnergySupplierNumber(ActorNumber expectedEnergySupplierNumber)
    {
        ArgumentNullException.ThrowIfNull(expectedEnergySupplierNumber);
        FirstTimeSeriesElement()
            .GetProperty("energySupplier_MarketParticipant.mRID")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedEnergySupplierNumber.Value);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasProductCode(string expectedProductCode)
    {
        FirstTimeSeriesElement().GetProperty("product").GetString().Should().Be(expectedProductCode);
        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasMeasurementUnit(MeasurementUnit expectedMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedMeasurementUnit);
        FirstTimeSeriesElement()
            .GetProperty("quantity_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedMeasurementUnit.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPriceMeasurementUnit(
        MeasurementUnit expectedPriceMeasurementUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceMeasurementUnit);
        FirstTimeSeriesElement()
            .GetProperty("price_Measure_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPriceMeasurementUnit.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasCurrency(Currency expectedPriceUnit)
    {
        ArgumentNullException.ThrowIfNull(expectedPriceUnit);
        FirstTimeSeriesElement()
            .GetProperty("currency_Unit.name")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPriceUnit.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPeriod(Period expectedPeriod)
    {
        ArgumentNullException.ThrowIfNull(expectedPeriod);
        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("start")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPeriod.StartToString());

        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("timeInterval")
            .GetProperty("end")
            .GetProperty("value")
            .GetString()
            .Should()
            .Be(expectedPeriod.EndToString());

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasResolution(Resolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("resolution")
            .GetString()
            .Should()
            .Be(resolution.Code);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument HasPositionAndQuantity(int expectedPosition, int expectedQuantity)
    {
        var point = FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[0];

        point
            .GetProperty("position")
            .GetProperty("value")
            .GetInt32()
            .Should()
            .Be(expectedPosition);

        point
            .GetProperty("energySum_Quantity.quantity")
            .GetDecimal()
            .Should()
            .Be(expectedQuantity);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementMethodIsNotPresent()
    {
        var act = () => FirstTimeSeriesElement()
            .GetProperty("marketEvaluationPoint.settlementMethod");

        act.Should()
            .ThrowExactly<KeyNotFoundException>()
            .WithMessage("The given key was not present in the dictionary.");

        return this;
    }

    public IAssertWholesaleCalculationResultDocument QualityIsPresentForPosition(
        int expectedPosition,
        string expectedQuantityQualityCode)
    {
        FirstTimeSeriesElement()
            .GetProperty("Period")
            .GetProperty("Point")
            .EnumerateArray()
            .ToList()[expectedPosition - 1]
            .GetProperty("quality")
            .GetString()
            .Should()
            .Be(expectedQuantityQualityCode);

        return this;
    }

    public IAssertWholesaleCalculationResultDocument SettlementVersionIsNotPresent()
    {
        var act = () => FirstTimeSeriesElement()
            .GetProperty("settlement_Series.version");

        act.Should()
            .ThrowExactly<KeyNotFoundException>()
            .WithMessage("The given key was not present in the dictionary.");

        return this;
    }

    private JsonElement FirstTimeSeriesElement()
    {
        return _root.GetProperty("Series").EnumerateArray().ToList()[0];
    }
}
